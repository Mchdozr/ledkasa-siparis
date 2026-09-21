using LedKasa.Siparis.Features.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace LedKasa.Siparis.Features.Reports;

public static class ReportingModule
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddSingleton<IExcelReportExporter, ExcelReportExporter>();
        services.AddSingleton<IPdfReportExporter, PdfReportExporter>();
        return services;
    }
}
