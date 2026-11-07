/*
 * Saranin
 *
 * Copyright (c) 2026 Althaf671
 *
 * Modification, redistribution, and commercial use are prohibited.
 *
 * See the project README for the complete license terms.
 */

namespace Saranin.Web.Pages.Dashboards.Shared.Models;

public sealed record ChartViewModel
{
    public string Label { get; init; } = string.Empty;
    public int Value { get; init; }
}