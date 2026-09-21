using System.Text.Json.Serialization;
using PSI1.Api.Models;

namespace PSI1.Api.DTOs;

public record LanguageSelectionRequest([property: JsonConverter(typeof(JsonStringEnumConverter))] Language Language);
