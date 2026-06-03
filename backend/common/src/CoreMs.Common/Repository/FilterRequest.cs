namespace CoreMs.Common.Repository;

public record FilterRequest(string Field, FilterOperation Operation, string Value);
