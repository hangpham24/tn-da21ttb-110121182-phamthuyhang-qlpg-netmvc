using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Models.DTOs
{
    // Custom Report DTOs
    public class CustomReportDto
    {
        public int? ReportId { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty; // "table", "chart", "mixed"
        public string UserRole { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        
        // Query Configuration
        public List<string> SelectedFields { get; set; } = new();
        public List<FilterConditionDto> Filters { get; set; } = new();
        public List<SortConditionDto> Sorting { get; set; } = new();
        public string GroupBy { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        // Chart Configuration
        public string ChartType { get; set; } = "line"; // line, bar, pie, doughnut, area
        public string XAxisField { get; set; } = string.Empty;
        public string YAxisField { get; set; } = string.Empty;
        public List<string> SeriesFields { get; set; } = new();
        
        // Export Configuration
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeSummary { get; set; } = true;
        public string ExportFormat { get; set; } = "pdf"; // pdf, excel
    }

    public class FilterConditionDto
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty; // eq, ne, gt, lt, gte, lte, contains, in
        public object Value { get; set; } = string.Empty;
        public string LogicalOperator { get; set; } = "AND"; // AND, OR
    }

    public class SortConditionDto
    {
        public string Field { get; set; } = string.Empty;
        public string Direction { get; set; } = "ASC"; // ASC, DESC
    }

    public class CustomReportResultDto
    {
        public string ReportName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public int TotalRecords { get; set; }
        public List<Dictionary<string, object>> Data { get; set; } = new();
        public Dictionary<string, object> Summary { get; set; } = new();
        public AdvancedChartDataDto? ChartData { get; set; }
    }

    public class CustomReportTemplateDto
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsPublic { get; set; }
        public CustomReportDto ReportConfig { get; set; } = new();
    }

    // Advanced Chart DTOs
    public class AdvancedChartDataDto
    {
        public string ChartType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<string> Labels { get; set; } = new();
        public List<ChartSeriesDto> Series { get; set; } = new();
        public ChartOptionsDto Options { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ChartSeriesDto
    {
        public string Name { get; set; } = string.Empty;
        public List<decimal> Data { get; set; } = new();
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // line, bar, area
        public bool Fill { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class ChartOptionsDto
    {
        public bool Responsive { get; set; } = true;
        public bool MaintainAspectRatio { get; set; } = false;
        public string XAxisLabel { get; set; } = string.Empty;
        public string YAxisLabel { get; set; } = string.Empty;
        public bool ShowLegend { get; set; } = true;
        public bool ShowTooltips { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
        public string CurrencyFormat { get; set; } = "VND";
        public Dictionary<string, object> CustomOptions { get; set; } = new();
    }

    // Dashboard Analytics DTOs
    public class DashboardAnalyticsDto
    {
        public List<KpiMetricDto> KpiMetrics { get; set; } = new();
        public List<AdvancedChartDataDto> Charts { get; set; } = new();
        public List<TrendDataDto> Trends { get; set; } = new();
        public List<AlertDto> Alerts { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class KpiMetricDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal? PreviousValue { get; set; }
        public decimal? ChangePercent { get; set; }
        public string ChangeDirection { get; set; } = string.Empty; // up, down, neutral
        public string Format { get; set; } = "number"; // number, currency, percentage
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TrendDataDto
    {
        public string Name { get; set; } = string.Empty;
        public List<TrendPointDto> Points { get; set; } = new();
        public string TrendDirection { get; set; } = string.Empty;
        public decimal TrendStrength { get; set; }
    }

    public class TrendPointDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class AlertDto
    {
        public string Type { get; set; } = string.Empty; // success, warning, error, info
        public string Message { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    // Comparison & Forecast DTOs
    public class ComparisonReportDto
    {
        public string MetricName { get; set; } = string.Empty;
        public PeriodDataDto Period1 { get; set; } = new();
        public PeriodDataDto Period2 { get; set; } = new();
        public decimal ChangeAmount { get; set; }
        public decimal ChangePercent { get; set; }
        public string ChangeDirection { get; set; } = string.Empty;
        public List<ComparisonDetailDto> Details { get; set; } = new();
    }

    public class PeriodDataDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Value { get; set; }
        public string Label { get; set; } = string.Empty;
        public Dictionary<string, decimal> Breakdown { get; set; } = new();
    }

    public class ComparisonDetailDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal Period1Value { get; set; }
        public decimal Period2Value { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }
    }

    public class ForecastDataDto
    {
        public string MetricName { get; set; } = string.Empty;
        public List<ForecastPointDto> Historical { get; set; } = new();
        public List<ForecastPointDto> Forecast { get; set; } = new();
        public decimal Confidence { get; set; }
        public string Model { get; set; } = string.Empty;
        public Dictionary<string, object> ModelParameters { get; set; } = new();
    }

    public class ForecastPointDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public decimal? LowerBound { get; set; }
        public decimal? UpperBound { get; set; }
        public bool IsActual { get; set; }
    }

    // Export DTOs
    public class ExportRequestDto
    {
        [Required]
        public string ReportType { get; set; } = string.Empty;
        
        [Required]
        public string Format { get; set; } = "pdf"; // pdf, excel
        
        public Dictionary<string, object> Parameters { get; set; } = new();
        public CustomReportDto? CustomReport { get; set; }
        
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeSummary { get; set; } = true;
        public string Template { get; set; } = "default";
    }

    // Alias for compatibility
    public class ReportTemplateDto
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string ReportFormat { get; set; } = "pdf";
        public List<string> AvailableMetrics { get; set; } = new();
        public Dictionary<string, object> DefaultParameters { get; set; } = new();
    }
} 