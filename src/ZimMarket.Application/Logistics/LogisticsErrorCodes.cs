namespace ZimMarket.Application.Logistics;

public static class LogisticsErrorCodes
{
    public const string LogisticsForbidden = "LOGISTICS_FORBIDDEN";

    public const string DriverNotFound = "LOGISTICS_DRIVER_NOT_FOUND";

    public const string DriverNotEligible = "LOGISTICS_DRIVER_NOT_ELIGIBLE";

    public const string DriverHasActiveBatch = "LOGISTICS_DRIVER_HAS_ACTIVE_BATCH";

    public const string OrderNotEligibleForBatch = "LOGISTICS_ORDER_NOT_ELIGIBLE";

    public const string OrderAlreadyBatched = "LOGISTICS_ORDER_ALREADY_BATCHED";

    public const string BatchCreateFailed = "LOGISTICS_BATCH_CREATE_FAILED";

    public const string DeliveryBatchNotFound = "LOGISTICS_BATCH_NOT_FOUND";

    public const string DeliveryBatchForbidden = "LOGISTICS_BATCH_FORBIDDEN";

    public const string DeliveryBatchInvalidState = "LOGISTICS_BATCH_INVALID_STATE";

    public const string OrderNotBatchedForCollection = "LOGISTICS_ORDER_NOT_BATCHED_FOR_COLLECTION";

    public const string OrderNotInDeliveryBatch = "LOGISTICS_ORDER_NOT_IN_DELIVERY_BATCH";

    public const string BatchNotReadyForDelivery = "LOGISTICS_BATCH_NOT_READY_FOR_DELIVERY";

    public const string OrderNotOutForDelivery = "LOGISTICS_ORDER_NOT_OUT_FOR_DELIVERY";

    public const string OrderAlreadyDelivered = "LOGISTICS_ORDER_ALREADY_DELIVERED";
}
