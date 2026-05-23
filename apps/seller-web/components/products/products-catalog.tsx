"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import {
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
  Download,
  Eye,
  MoreHorizontal,
  Package,
  Pencil,
  Search,
  SlidersHorizontal,
  Trash2,
} from "lucide-react";

import { ProductImage } from "@/components/products/product-image";
import { ProductStatusBadge } from "@/components/products/product-status-badge";
import { DeleteProductDialog } from "@/components/products/delete-product-dialog";
import { ApiError } from "@/lib/api";
import { formatCurrencyUsd } from "@/lib/domain-enums";
import {
  daysUntilPermanentDeletion,
  sellerProductsService,
  type SellerProductSummary,
} from "@/lib/seller-products";
import { cn } from "@/lib/utils";

export type ProductStatusTab = "all" | "active" | "draft" | "archived";

type SortKey = "title" | "category" | "status" | "stock" | "price" | "created";
type SortDirection = "asc" | "desc";
type ColumnKey = "category" | "status" | "stock" | "price" | "created";

const pageSize = 20;
const productMenuWidthPx = 160;

function getProductMenuPosition(anchor: HTMLElement): { top: number; left: number } {
  const rect = anchor.getBoundingClientRect();

  return {
    top: rect.bottom + 4,
    left: Math.max(8, rect.right - productMenuWidthPx),
  };
}

const statusTabs: { id: ProductStatusTab; label: string }[] = [
  { id: "all", label: "All" },
  { id: "active", label: "Active" },
  { id: "draft", label: "Draft" },
  { id: "archived", label: "Archived" },
];

const defaultVisibleColumns: Record<ColumnKey, boolean> = {
  category: true,
  status: true,
  stock: true,
  price: true,
  created: true,
};

function resolveStatus(status: number | string): number {
  return typeof status === "string" ? Number.parseInt(status, 10) : status;
}

function tabToApiScope(tab: ProductStatusTab): "active" | "deleted" | "all" {
  if (tab === "archived") {
    return "deleted";
  }

  if (tab === "all") {
    return "all";
  }

  return "active";
}

function matchesStatusTab(product: SellerProductSummary, tab: ProductStatusTab): boolean {
  const status = resolveStatus(product.status);

  switch (tab) {
    case "all":
      return status !== 2;
    case "active":
      return status === 0;
    case "draft":
      return status === 1;
    case "archived":
      return status === 2;
    default:
      return true;
  }
}

function formatCreatedDate(value: string): string {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

function compareProducts(a: SellerProductSummary, b: SellerProductSummary, key: SortKey): number {
  switch (key) {
    case "title":
      return a.title.localeCompare(b.title);
    case "category":
      return a.categoryName.localeCompare(b.categoryName);
    case "status":
      return resolveStatus(a.status) - resolveStatus(b.status);
    case "stock":
      return a.stockQuantity - b.stockQuantity;
    case "price":
      return a.priceAmount - b.priceAmount;
    case "created":
      return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
    default:
      return 0;
  }
}

function exportProductsCsv(products: SellerProductSummary[]): void {
  const header = ["Title", "Category", "Status", "Stock", "Price", "Created"];
  const rows = products.map((product) => [
    product.title,
    product.categoryName,
    String(resolveStatus(product.status)),
    String(product.stockQuantity),
    String(product.priceAmount),
    product.createdAt,
  ]);

  const csv = [header, ...rows]
    .map((row) => row.map((cell) => `"${cell.replaceAll('"', '""')}"`).join(","))
    .join("\n");

  const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "products.csv";
  link.click();
  URL.revokeObjectURL(url);
}

type ProductsCatalogProps = {
  kycApproved: boolean;
};

export function ProductsCatalog({ kycApproved }: ProductsCatalogProps) {
  const [statusTab, setStatusTab] = useState<ProductStatusTab>("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [categoryFilter, setCategoryFilter] = useState<string>("all");
  const [visibleColumns, setVisibleColumns] = useState(defaultVisibleColumns);
  const [showColumnsMenu, setShowColumnsMenu] = useState(false);
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const [menuPosition, setMenuPosition] = useState<{ top: number; left: number } | null>(null);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [products, setProducts] = useState<SellerProductSummary[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [sortKey, setSortKey] = useState<SortKey>("created");
  const [sortDirection, setSortDirection] = useState<SortDirection>("desc");
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<{ productId: string; title: string } | null>(
    null,
  );
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const columnsMenuRef = useRef<HTMLDivElement>(null);
  const menuAnchorRef = useRef<HTMLElement | null>(null);

  const closeProductMenu = useCallback(() => {
    setOpenMenuId(null);
    setMenuPosition(null);
    menuAnchorRef.current = null;
  }, []);

  const toggleProductMenu = useCallback(
    (productId: string, anchor: HTMLElement) => {
      if (openMenuId === productId) {
        closeProductMenu();
        return;
      }

      menuAnchorRef.current = anchor;
      setMenuPosition(getProductMenuPosition(anchor));
      setOpenMenuId(productId);
    },
    [closeProductMenu, openMenuId],
  );

  const loadProducts = useCallback(async () => {
    try {
      const response = await sellerProductsService.listProducts({
        page,
        pageSize,
        scope: tabToApiScope(statusTab),
      });

      setProducts(response.items);
      setTotalCount(response.totalCount);
      setErrorMessage(null);
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("Unable to load products.");
      }
    } finally {
      setIsLoading(false);
    }
  }, [page, statusTab]);

  useEffect(() => {
    setIsLoading(true);
    setSelectedIds(new Set());
    closeProductMenu();
    void loadProducts();
  }, [closeProductMenu, loadProducts]);

  useEffect(() => {
    const onVisible = () => {
      if (document.visibilityState === "visible") {
        void loadProducts();
      }
    };

    window.addEventListener("focus", onVisible);
    document.addEventListener("visibilitychange", onVisible);

    return () => {
      window.removeEventListener("focus", onVisible);
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, [loadProducts]);

  useEffect(() => {
    function onDocumentClick(event: MouseEvent) {
      if (!columnsMenuRef.current?.contains(event.target as Node)) {
        setShowColumnsMenu(false);
      }

      if (!(event.target as HTMLElement).closest("[data-product-menu]")) {
        closeProductMenu();
      }
    }

    document.addEventListener("click", onDocumentClick);
    return () => document.removeEventListener("click", onDocumentClick);
  }, [closeProductMenu]);

  useEffect(() => {
    if (!openMenuId || !menuAnchorRef.current) {
      return;
    }

    const reposition = () => {
      if (menuAnchorRef.current) {
        setMenuPosition(getProductMenuPosition(menuAnchorRef.current));
      }
    };

    window.addEventListener("scroll", reposition, true);
    window.addEventListener("resize", reposition);

    return () => {
      window.removeEventListener("scroll", reposition, true);
      window.removeEventListener("resize", reposition);
    };
  }, [openMenuId]);

  const categories = useMemo(() => {
    const unique = new Map<string, string>();
    for (const product of products) {
      unique.set(product.categoryId, product.categoryName);
    }

    return Array.from(unique.entries()).map(([id, name]) => ({ id, name }));
  }, [products]);

  const filteredProducts = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();

    return products
      .filter((product) => matchesStatusTab(product, statusTab))
      .filter((product) => categoryFilter === "all" || product.categoryId === categoryFilter)
      .filter((product) => {
        if (!query) {
          return true;
        }

        return (
          product.title.toLowerCase().includes(query) ||
          (product.description ?? "").toLowerCase().includes(query) ||
          product.categoryName.toLowerCase().includes(query)
        );
      })
      .sort((a, b) => {
        const direction = sortDirection === "asc" ? 1 : -1;
        return compareProducts(a, b, sortKey) * direction;
      });
  }, [products, statusTab, searchQuery, categoryFilter, sortKey, sortDirection]);

  const openMenuProduct = useMemo(
    () => filteredProducts.find((product) => product.productId === openMenuId) ?? null,
    [filteredProducts, openMenuId],
  );

  const allVisibleSelected =
    filteredProducts.length > 0 &&
    filteredProducts.every((product) => selectedIds.has(product.productId));

  const toggleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortDirection((current) => (current === "asc" ? "desc" : "asc"));
      return;
    }

    setSortKey(key);
    setSortDirection("asc");
  };

  const SortIcon = ({ column }: { column: SortKey }) => {
    if (sortKey !== column) {
      return <ArrowUpDown className="size-3.5 opacity-40" />;
    }

    return sortDirection === "asc" ? (
      <ArrowUp className="size-3.5" />
    ) : (
      <ArrowDown className="size-3.5" />
    );
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const handleDeleteConfirm = async () => {
    if (!deleteTarget) {
      return;
    }

    setDeletingId(deleteTarget.productId);
    setDeleteError(null);

    try {
      await sellerProductsService.deleteProduct(deleteTarget.productId);
      setDeleteTarget(null);
      await loadProducts();
    } catch (error) {
      setDeleteError(error instanceof Error ? error.message : "Unable to delete product.");
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
        <div className="flex flex-wrap gap-2">
          {statusTabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => {
                setStatusTab(tab.id);
                setPage(1);
              }}
              className={cn(
                "rounded-lg px-4 py-2 text-sm font-medium transition-colors",
                statusTab === tab.id
                  ? "bg-muted text-foreground shadow-sm"
                  : "text-muted-foreground hover:bg-muted/60 hover:text-foreground",
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <div className="relative min-w-[220px] flex-1 sm:min-w-[280px]">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="search"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search products..."
              className="h-10 w-full rounded-lg border border-border/80 bg-background pl-10 pr-3 text-sm outline-none focus:border-foreground/30 focus:ring-2 focus:ring-foreground/5"
            />
          </div>

          <select
            value={categoryFilter}
            onChange={(event) => setCategoryFilter(event.target.value)}
            className="h-10 rounded-lg border border-border/80 bg-background px-3 text-sm outline-none focus:border-foreground/30"
          >
            <option value="all">Category</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>

          <div className="relative" ref={columnsMenuRef}>
            <button
              type="button"
              onClick={() => setShowColumnsMenu((current) => !current)}
              className="inline-flex h-10 items-center gap-2 rounded-lg border border-border/80 bg-background px-3 text-sm font-medium hover:bg-muted/50"
            >
              <SlidersHorizontal className="size-4" />
              Columns
            </button>
            {showColumnsMenu ? (
              <div className="absolute right-0 z-20 mt-2 w-44 rounded-xl border border-border/80 bg-card p-2 shadow-lg">
                {(Object.keys(defaultVisibleColumns) as ColumnKey[]).map((column) => (
                  <label
                    key={column}
                    className="flex cursor-pointer items-center gap-2 rounded-lg px-2 py-2 text-sm hover:bg-muted/60"
                  >
                    <input
                      type="checkbox"
                      checked={visibleColumns[column]}
                      onChange={(event) =>
                        setVisibleColumns((current) => ({
                          ...current,
                          [column]: event.target.checked,
                        }))
                      }
                    />
                    <span className="capitalize">{column}</span>
                  </label>
                ))}
              </div>
            ) : null}
          </div>

          <button
            type="button"
            onClick={() => exportProductsCsv(filteredProducts)}
            className="inline-flex h-10 items-center gap-2 rounded-lg border border-border/80 bg-background px-3 text-sm font-medium hover:bg-muted/50"
          >
            <Download className="size-4" />
            Export
          </button>
        </div>
      </div>

      {errorMessage ? (
        <div className="rounded-xl border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {errorMessage}
        </div>
      ) : null}

      <section className="overflow-hidden rounded-xl border border-border/80 bg-card">
        <div className="overflow-x-auto">
          <table className="min-w-full">
            <thead className="border-b border-border/80 bg-muted/30">
              <tr className="text-left text-sm font-medium text-muted-foreground">
                <th className="w-12 px-4 py-3">
                  <input
                    type="checkbox"
                    checked={allVisibleSelected}
                    onChange={(event) => {
                      if (event.target.checked) {
                        setSelectedIds(new Set(filteredProducts.map((product) => product.productId)));
                        return;
                      }

                      setSelectedIds(new Set());
                    }}
                    aria-label="Select all products"
                  />
                </th>
                <th className="min-w-[280px] px-4 py-3">
                  <button
                    type="button"
                    onClick={() => toggleSort("title")}
                    className="inline-flex items-center gap-1.5 hover:text-foreground"
                  >
                    Product
                    <SortIcon column="title" />
                  </button>
                </th>
                {visibleColumns.category ? (
                  <th className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => toggleSort("category")}
                      className="inline-flex items-center gap-1.5 hover:text-foreground"
                    >
                      Category
                      <SortIcon column="category" />
                    </button>
                  </th>
                ) : null}
                {visibleColumns.status ? (
                  <th className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => toggleSort("status")}
                      className="inline-flex items-center gap-1.5 hover:text-foreground"
                    >
                      Status
                      <SortIcon column="status" />
                    </button>
                  </th>
                ) : null}
                {visibleColumns.stock ? (
                  <th className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => toggleSort("stock")}
                      className="inline-flex items-center gap-1.5 hover:text-foreground"
                    >
                      Stock
                      <SortIcon column="stock" />
                    </button>
                  </th>
                ) : null}
                {visibleColumns.price ? (
                  <th className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => toggleSort("price")}
                      className="inline-flex items-center gap-1.5 hover:text-foreground"
                    >
                      Price
                      <SortIcon column="price" />
                    </button>
                  </th>
                ) : null}
                {visibleColumns.created ? (
                  <th className="px-4 py-3">
                    <button
                      type="button"
                      onClick={() => toggleSort("created")}
                      className="inline-flex items-center gap-1.5 hover:text-foreground"
                    >
                      Created
                      <SortIcon column="created" />
                    </button>
                  </th>
                ) : null}
                <th className="w-12 px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-border/60 text-sm">
              {filteredProducts.map((product) => (
                <tr key={product.productId} className="hover:bg-muted/20">
                  <td className="px-4 py-4">
                    <input
                      type="checkbox"
                      checked={selectedIds.has(product.productId)}
                      onChange={(event) => {
                        setSelectedIds((current) => {
                          const next = new Set(current);
                          if (event.target.checked) {
                            next.add(product.productId);
                          } else {
                            next.delete(product.productId);
                          }

                          return next;
                        });
                      }}
                      aria-label={`Select ${product.title}`}
                    />
                  </td>
                  <td className="px-4 py-4">
                    <div className="flex items-center gap-3">
                      <div className="relative flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-border/70 bg-muted">
                        <ProductImage imageKey={product.primaryImageKey} alt={product.title} />
                      </div>
                      <div className="min-w-0">
                        <p className="truncate font-medium text-foreground">{product.title}</p>
                        <p className="truncate text-xs text-muted-foreground">{product.description}</p>
                      </div>
                    </div>
                  </td>
                  {visibleColumns.category ? (
                    <td className="px-4 py-4 text-muted-foreground">{product.categoryName}</td>
                  ) : null}
                  {visibleColumns.status ? (
                    <td className="px-4 py-4">
                      <ProductStatusBadge status={product.status} />
                      {statusTab === "archived" ? (
                        <p className="mt-1 text-xs text-muted-foreground">
                          Purge in {daysUntilPermanentDeletion(product.updatedAt)} days
                        </p>
                      ) : null}
                    </td>
                  ) : null}
                  {visibleColumns.stock ? (
                    <td className="px-4 py-4 tabular-nums">{product.stockQuantity}</td>
                  ) : null}
                  {visibleColumns.price ? (
                    <td className="px-4 py-4 tabular-nums font-medium">
                      {formatCurrencyUsd(product.priceAmount)}
                    </td>
                  ) : null}
                  {visibleColumns.created ? (
                    <td className="px-4 py-4 text-muted-foreground">
                      {formatCreatedDate(product.createdAt)}
                    </td>
                  ) : null}
                  <td className="relative px-4 py-4" data-product-menu>
                    <button
                      type="button"
                      onClick={(event) => toggleProductMenu(product.productId, event.currentTarget)}
                      className="inline-flex size-8 items-center justify-center rounded-lg hover:bg-muted/70"
                      aria-label="Product actions"
                      aria-expanded={openMenuId === product.productId}
                    >
                      <MoreHorizontal className="size-4" />
                    </button>
                  </td>
                </tr>
              ))}
              {!isLoading && filteredProducts.length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-4 py-16 text-center text-sm text-muted-foreground">
                    No products match your filters.
                  </td>
                </tr>
              ) : null}
              {isLoading ? (
                <tr>
                  <td colSpan={8} className="px-4 py-16 text-center text-sm text-muted-foreground">
                    Loading products…
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>

        {totalPages > 1 ? (
          <div className="flex items-center justify-between border-t border-border/80 px-4 py-3 text-sm">
            <p className="text-muted-foreground">
              Page {page} of {totalPages}
            </p>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={page <= 1 || isLoading}
                onClick={() => setPage((current) => Math.max(1, current - 1))}
                className="rounded-lg border border-border/80 px-3 py-1.5 disabled:opacity-50"
              >
                Previous
              </button>
              <button
                type="button"
                disabled={page >= totalPages || isLoading}
                onClick={() => setPage((current) => current + 1)}
                className="rounded-lg border border-border/80 px-3 py-1.5 disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        ) : null}
      </section>

      {openMenuProduct && menuPosition
        ? createPortal(
            <div
              data-product-menu
              className="fixed z-50 w-40 rounded-xl border border-border/80 bg-card py-1 shadow-lg"
              style={{ top: menuPosition.top, left: menuPosition.left }}
            >
              <Link
                href={`/products/${openMenuProduct.productId}`}
                onClick={closeProductMenu}
                className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-muted/60"
              >
                <Eye className="size-4 text-muted-foreground" />
                View
              </Link>
              {statusTab !== "archived" && kycApproved ? (
                <Link
                  href={`/products/${openMenuProduct.productId}/edit`}
                  onClick={closeProductMenu}
                  className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-muted/60"
                >
                  <Pencil className="size-4 text-muted-foreground" />
                  Edit
                </Link>
              ) : null}
              {statusTab !== "archived" && kycApproved ? (
                <button
                  type="button"
                  disabled={deletingId === openMenuProduct.productId}
                  onClick={() => {
                    closeProductMenu();
                    setDeleteError(null);
                    setDeleteTarget({
                      productId: openMenuProduct.productId,
                      title: openMenuProduct.title,
                    });
                  }}
                  className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm text-destructive hover:bg-destructive/10 disabled:opacity-50"
                >
                  <Trash2 className="size-4" />
                  Delete
                </button>
              ) : null}
            </div>,
            document.body,
          )
        : null}

      <DeleteProductDialog
        open={deleteTarget !== null}
        productTitle={deleteTarget?.title}
        isDeleting={deletingId !== null}
        errorMessage={deleteError}
        onConfirm={() => void handleDeleteConfirm()}
        onCancel={() => {
          if (deletingId === null) {
            setDeleteTarget(null);
            setDeleteError(null);
          }
        }}
      />
    </div>
  );
}
