using System;

namespace WpfSample.Models
{
    public record LogModel(string Type, DateTime Time, string Message);
}