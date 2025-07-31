using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CohesionX.UserManagement.Abstractions.DTOs.Options;

/// <summary>
/// Represents configuration options for OpenTelemetry instrumentation in an application.
/// </summary>
/// <remarks>These options are used to configure OpenTelemetry tracing, including the service name,  service
/// version, and connection string for Azure Application Insights.</remarks>
public class OpenTelemetryOptions
{
    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    required public string ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the version of the service.
    /// </summary>
    required public string ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the connection string for Azure Application Insights.
    /// </summary>
    required public string AzureApplicationInsightsConnStr { get; set; }
}