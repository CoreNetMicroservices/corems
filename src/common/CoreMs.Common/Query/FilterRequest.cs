namespace CoreMs.Common.Query;

public record FilterRequest(string Field, FilterOperation Operation, string Value);
