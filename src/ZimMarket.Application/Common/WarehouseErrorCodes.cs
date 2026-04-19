namespace ZimMarket.Application.Common;

public static class WarehouseErrorCodes
{
    public const string WarehouseItemNotFound = "WAREHOUSE_ITEM_NOT_FOUND";
    public const string WarehouseQcInvalid = "WAREHOUSE_QC_INVALID";
    public const string WarehouseForbidden = "WAREHOUSE_FORBIDDEN";
    public const string OrderInvalidStatusForQc = "ORDER_INVALID_STATUS_FOR_QC";
}
