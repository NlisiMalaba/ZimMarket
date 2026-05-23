"use client";

import type { ComponentType } from "react";
import { useId, useMemo } from "react";

import { cn } from "@/lib/utils";

function mulberry32(seed: number): () => number {
  return () => {
    let t = (seed += 0x6d2b79f5);
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function seededSeries(seed: number, length: number, min: number, max: number): number[] {
  const rand = mulberry32(seed);
  return Array.from({ length }, () => min + rand() * (max - min));
}

export function Sparkline({
  seed,
  colorClass,
  className,
}: {
  seed: number;
  colorClass: string;
  className?: string;
}) {
  const gradId = useId().replace(/[^a-zA-Z0-9_-]/g, "");
  const points = useMemo(() => seededSeries(seed, 14, 4, 22), [seed]);
  const coords = points.map((y, i) => `${(i / (points.length - 1)) * 100},${26 - y}`).join(" ");

  return (
    <svg viewBox="0 0 100 26" className={cn("h-10 w-full", className)} preserveAspectRatio="none" aria-hidden>
      <defs>
        <linearGradient id={gradId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" className={cn("[stop-color:currentColor]", colorClass)} stopOpacity={0.35} />
          <stop offset="100%" className={cn("[stop-color:currentColor]", colorClass)} stopOpacity={0.02} />
        </linearGradient>
      </defs>
      <polygon fill={`url(#${gradId})`} points={`0,26 ${coords} 100,26`} className={colorClass} />
      <polyline
        fill="none"
        strokeWidth={2}
        strokeLinecap="round"
        strokeLinejoin="round"
        points={coords}
        className={cn("stroke-current", colorClass)}
      />
    </svg>
  );
}

function trendFromSeed(seed: number): { label: string; positive: boolean } {
  const v = (seed % 35) - 8;
  return {
    label: `${v >= 0 ? "+" : ""}${v.toFixed(1)}%`,
    positive: v >= 0,
  };
}

export function MetricHighlightCard({
  title,
  value,
  seed,
  accent,
  icon: Icon,
}: {
  title: string;
  value: string;
  seed: number;
  accent: "orange" | "teal" | "slate" | "amber";
  icon: ComponentType<{ className?: string }>;
}) {
  const trend = trendFromSeed(seed);
  const accentMap = {
    orange: "text-orange-500",
    teal: "text-teal-500",
    slate: "text-slate-700 dark:text-slate-300",
    amber: "text-amber-500",
  } as const;

  const iconBg = {
    orange: "bg-orange-500/15 text-orange-600 dark:text-orange-400",
    teal: "bg-teal-500/15 text-teal-600 dark:text-teal-400",
    slate: "bg-slate-500/15 text-slate-700 dark:text-slate-300",
    amber: "bg-amber-500/15 text-amber-600 dark:text-amber-400",
  } as const;

  return (
    <article className="flex flex-col rounded-2xl border border-border/70 bg-card p-5 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-medium text-muted-foreground">{title}</p>
          <p className="mt-2 text-3xl font-semibold tracking-tight text-foreground">{value}</p>
          <p
            className={cn(
              "mt-1 text-xs font-semibold",
              trend.positive ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400",
            )}
          >
            {trend.label}
            <span className="ml-1 font-normal text-muted-foreground">vs last month</span>
          </p>
        </div>
        <span className={cn("flex size-11 items-center justify-center rounded-xl", iconBg[accent])}>
          <Icon className="size-5" aria-hidden />
        </span>
      </div>
      <div className="mt-4">
        <Sparkline seed={seed} colorClass={accentMap[accent]} />
      </div>
    </article>
  );
}

export function OverviewAreaChart({
  revenueUsd,
  seed,
}: {
  revenueUsd: number;
  seed: number;
}) {
  const series = useMemo(() => {
    const base = Math.max(revenueUsd, 1);
    const rnd = mulberry32(seed);
    return Array.from({ length: 12 }, (_, month) => {
      const seasonal = 0.65 + 0.35 * Math.sin((month / 11) * Math.PI);
      const jitter = 0.85 + rnd() * 0.3;
      return base * seasonal * jitter * (1.1 + month * 0.04);
    });
  }, [revenueUsd, seed]);

  const max = Math.max(...series, 1);
  const width = 320;
  const height = 120;
  const pad = 8;
  const pts = series.map((v, i) => {
    const x = pad + (i / (series.length - 1)) * (width - pad * 2);
    const y = height - pad - (v / max) * (height - pad * 2);
    return [x, y] as const;
  });

  const pathD = pts.map(([x, y], i) => `${i === 0 ? "M" : "L"} ${x} ${y}`).join(" ");
  const areaD = `${pathD} L ${pts[pts.length - 1]?.[0]} ${height} L ${pts[0]?.[0]} ${height} Z`;
  const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

  return (
    <div className="rounded-2xl border border-border/70 bg-card p-6 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight">Overview</h2>
          <p className="mt-1 text-sm text-muted-foreground">Monthly performance for your store</p>
        </div>
        <div className="flex rounded-xl border border-border/80 bg-muted/30 p-1 text-xs font-medium">
          <span className="rounded-lg bg-card px-3 py-1.5 shadow-sm">Revenue</span>
          <span className="px-3 py-1.5 text-muted-foreground">Orders</span>
          <span className="px-3 py-1.5 text-muted-foreground">Listings</span>
        </div>
      </div>

      <div className="mt-6 overflow-x-auto">
        <svg viewBox={`0 0 ${width} ${height}`} className="w-full min-w-[280px]" role="img" aria-label="Overview chart">
          <defs>
            <linearGradient id="seller-overview-fill" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="rgb(249 115 22)" stopOpacity={0.35} />
              <stop offset="100%" stopColor="rgb(249 115 22)" stopOpacity={0} />
            </linearGradient>
          </defs>
          <path d={areaD} fill="url(#seller-overview-fill)" />
          <path
            d={pathD}
            fill="none"
            stroke="rgb(249 115 22)"
            strokeWidth={2.5}
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
        <div className="mt-2 flex justify-between gap-1 px-1 text-[10px] font-medium text-muted-foreground">
          {months.map((m) => (
            <span key={m} className="flex-1 text-center">
              {m}
            </span>
          ))}
        </div>
      </div>
    </div>
  );
}

export function OrderMixDonut({
  title,
  subtitle,
  centerLabel,
  centerCaption,
  slices,
}: {
  title: string;
  subtitle: string;
  centerLabel: string;
  centerCaption: string;
  slices: { label: string; value: number; color: string }[];
}) {
  const total = slices.reduce((s, x) => s + Math.max(0, x.value), 0);
  const safeTotal = total > 0 ? total : 1;

  const conicStops = useMemo(() => {
    const positive = slices.filter((s) => s.value > 0);
    const { segments } = positive.reduce(
      (acc, slice) => {
        const pct = (slice.value / safeTotal) * 100;
        const next = acc.cursor + pct;
        return {
          cursor: next,
          segments: [...acc.segments, `${slice.color} ${acc.cursor}% ${next}%`],
        };
      },
      { cursor: 0, segments: [] as string[] },
    );
    return segments.join(", ");
  }, [slices, safeTotal]);

  return (
    <div className="rounded-2xl border border-border/70 bg-card p-6 shadow-sm">
      <h2 className="text-lg font-semibold tracking-tight">{title}</h2>
      <p className="mt-1 text-sm text-muted-foreground">{subtitle}</p>

      <div className="mt-6 flex flex-col items-center gap-6 sm:flex-row sm:items-center sm:justify-center">
        <div className="relative size-44 shrink-0">
          <div
            className="size-full rounded-full shadow-inner ring-1 ring-black/5 dark:ring-white/10"
            style={{
              background:
                conicStops.length > 0
                  ? `conic-gradient(${conicStops})`
                  : "conic-gradient(var(--muted) 0% 100%)",
            }}
          />
          <div className="pointer-events-none absolute inset-[22%] flex flex-col items-center justify-center rounded-full bg-card text-center shadow-sm ring-1 ring-border/60">
            <span className="text-2xl font-semibold tracking-tight">{centerLabel}</span>
            <span className="text-xs text-muted-foreground">{centerCaption}</span>
          </div>
        </div>

        <ul className="w-full max-w-[220px] space-y-3 text-sm">
          {slices.map((slice) => (
            <li key={slice.label} className="flex items-center justify-between gap-3">
              <span className="flex items-center gap-2 text-muted-foreground">
                <span className="size-2.5 rounded-full" style={{ backgroundColor: slice.color }} />
                {slice.label}
              </span>
              <span className="font-semibold tabular-nums text-foreground">
                {total > 0 ? Math.round((slice.value / safeTotal) * 100) : 0}%
              </span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

export function MonthlyGoalCard({
  title,
  subtitle,
  current,
  target,
  formatter,
}: {
  title: string;
  subtitle: string;
  current: number;
  target: number;
  formatter: (n: number) => string;
}) {
  const pct = target > 0 ? Math.min(100, Math.round((current / target) * 100)) : 0;

  return (
    <div className="rounded-2xl border border-border/70 bg-card p-6 shadow-sm">
      <h2 className="text-lg font-semibold tracking-tight">{title}</h2>
      <p className="mt-1 text-sm text-muted-foreground">{subtitle}</p>
      <div className="mt-6">
        <div className="h-3 overflow-hidden rounded-full bg-muted">
          <div
            className="h-full rounded-full bg-gradient-to-r from-orange-400 to-amber-500 transition-[width] duration-500"
            style={{ width: `${pct}%` }}
          />
        </div>
        <div className="mt-3 flex items-center justify-between text-sm">
          <span className="font-semibold tabular-nums text-foreground">{formatter(current)}</span>
          <span className="text-muted-foreground">Target: {formatter(target)}</span>
        </div>
        <p className="mt-2 text-xs font-medium text-muted-foreground">{pct}% of goal</p>
      </div>
    </div>
  );
}
