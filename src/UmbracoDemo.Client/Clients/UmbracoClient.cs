﻿using UmbracoDemo.Client.Helpers;
using UmbracoDemo.Client.Models.Pages.Abstractions;

namespace UmbracoDemo.Client.Clients
{
    public interface IUmbracoClient
    {
        Task<BasePage?> GetContentByPath(string path, bool preview = false);

        Task<T?> GetContentSingleByType<T>(string? culture = null, bool preview = false,
            CancellationToken cancellationToken = default) where T : class, IContent, new();

        Task<ICollection<BasePage>> GetChildrenByPath(string path, string[]? sort = null, bool preview = false,
            CancellationToken cancellationToken = default);

        Task<(ICollection<BasePage> Pages, long Total)> Search(string query, int skip, int take, string? culture = null, bool preview = false,
            CancellationToken cancellationToken = default);
    }

    internal class UmbracoClient : IUmbracoClient
    {
        private readonly IUmbracoApi _umbracoApi;
        private readonly string? _apiKey;

        public UmbracoClient(IUmbracoApi umbracoApi, IConfiguration config)
        {
            _umbracoApi = umbracoApi;
            _apiKey = config["UmbracoApi:ApiKey"];
        }

        public async Task<BasePage?> GetContentByPath(string path, bool preview = false)
        {
            try
            {
                var response = await _umbracoApi.GetContentItemByPathAsync(path);
                return response.ConvertToPage(preview);
            }
            catch (ApiException e) when (e.StatusCode == 404)
            {
                return null;
            }
        }

        public async Task<T?> GetContentSingleByType<T>(string? culture = null, bool preview = false, CancellationToken cancellationToken = default) where T : class, IContent, new()
        {
            try
            {
                var response = await _umbracoApi.GetContentAsync(filter: new[] { $"contentType:{T.ContentTypeAlias}" }, take: 1, accept_Language: culture, preview: preview, api_Key: preview ? _apiKey : null, cancellationToken: cancellationToken);
                return response.Items.FirstOrDefault()?.ToContent<T>(preview);
            }
            catch (ApiException e) when (e.StatusCode == 404)
            {
                return null;
            }
        }

        public async Task<ICollection<BasePage>> GetChildrenByPath(string path, string[]? sort = null, bool preview = false, CancellationToken cancellationToken = default)
        {
            var response = await _umbracoApi.GetContentAsync(fetch: $"children:{path}", sort: sort ?? new[] { "sortOrder:asc" }, preview: preview, api_Key: preview ? _apiKey : null, cancellationToken: cancellationToken);
            return response.Items
                .Select(i => i.ConvertToPage(preview))
                .OfType<BasePage>()
                .ToList();
        }

        public async Task<(ICollection<BasePage> Pages, long Total)> Search(string query, int skip, int take, string? culture = null, bool preview = false,
            CancellationToken cancellationToken = default)
        {
            PagedIApiContentResponseModel response = await _umbracoApi.GetContentAsync(filter: new[] { $"search:{query}" }, skip: skip, take: take, accept_Language: culture, preview: preview, api_Key: preview ? _apiKey : null, cancellationToken: cancellationToken);
            return (response.Items
                .Select(i => i.ConvertToPage(preview))
                .OfType<BasePage>()
                .ToList(), response.Total);
        }
    }
}
