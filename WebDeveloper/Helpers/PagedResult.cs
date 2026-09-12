namespace WebDeveloper.Helpers
{
    /// <summary>
    /// Generic paged result wrapper (thay thế Spring Data Page<T>)
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Content { get; set; } = new();
        public int Page { get; set; }
        public int Size { get; set; }
        public long TotalElements { get; set; }
        public int TotalPages { get; set; }
        public bool First { get; set; }
        public bool Last { get; set; }

        public static PagedResult<T> Create(List<T> items, int page, int size, long totalElements)
        {
            return new PagedResult<T>
            {
                Content = items,
                Page = page,
                Size = size,
                TotalElements = totalElements,
                TotalPages = (int)Math.Ceiling(totalElements / (double)size),
                First = page == 0,
                Last = (page + 1) * size >= totalElements
            };
        }
    }
}
