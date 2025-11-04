namespace MyMarketManager.Processing;

internal record WorkItemHandlerRegistration(
    Type HandlerType,
    string Name,
    int MaxItemsPerCycle,
    WorkItemHandlerPurpose Purpose);
