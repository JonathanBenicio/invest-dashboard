using System;
using System.Collections.Generic;

namespace InvestDashboard.Application.DTOs.Common
{
    public class PaginationMetadata
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public bool Success { get; set; }
        public string? Message { get; set; }
        public PaginationMetadata Pagination { get; set; } = new();

        public PaginatedResponse() { }

        public PaginatedResponse(List<T> data, int page, int pageSize, int totalCount, bool success = true, string? message = null)
        {
            Data = data;
            Success = success;
            Message = message;
            
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            Pagination = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }
    }
}
