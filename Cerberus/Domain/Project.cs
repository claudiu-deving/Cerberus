﻿namespace Cerberus.Domain;

public record Project(Guid Id, string Name, string Description, Environment Environment,IEnumerable<Anima> Animas);
public record Tenant(Guid Id, string Name , IEnumerable<Project> Projects);
public record Anima(string Definition,string Value,string Description);