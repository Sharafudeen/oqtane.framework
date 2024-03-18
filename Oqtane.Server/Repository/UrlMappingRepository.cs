using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Oqtane.Models;

namespace Oqtane.Repository
{
    public class UrlMappingRepository : IUrlMappingRepository
    {
        private readonly IDbContextFactory<TenantDBContext> _dbContextFactory;
        private readonly TenantDBContext _queryContext;
        private readonly ISiteRepository _sites;

        public UrlMappingRepository(IDbContextFactory<TenantDBContext> dbContextFactory, ISiteRepository sites)
        {
            _dbContextFactory = dbContextFactory;
            _queryContext = _dbContextFactory.CreateDbContext();
            _sites = sites;
        }

        public IEnumerable<UrlMapping> GetUrlMappings(int siteId, bool isMapped)
        {
            if (isMapped)
            {
                return _queryContext.UrlMapping.Where(item => item.SiteId == siteId && !string.IsNullOrEmpty(item.MappedUrl)).Take(200);
            }
            else
            {
                return _queryContext.UrlMapping.Where(item => item.SiteId == siteId && string.IsNullOrEmpty(item.MappedUrl)).Take(200);
            }
        }

        public UrlMapping AddUrlMapping(UrlMapping urlMapping)
        {
            using var db = _dbContextFactory.CreateDbContext();
            db.UrlMapping.Add(urlMapping);
            db.SaveChanges();
            return urlMapping;
        }

        public UrlMapping UpdateUrlMapping(UrlMapping urlMapping)
        {
            using var db = _dbContextFactory.CreateDbContext();
            db.Entry(urlMapping).State = EntityState.Modified;
            db.SaveChanges();
            return urlMapping;
        }

        public UrlMapping GetUrlMapping(int urlMappingId)
        {
            using var db = _dbContextFactory.CreateDbContext();
            return GetUrlMapping(urlMappingId, true);
        }

        public UrlMapping GetUrlMapping(int urlMappingId, bool tracking)
        {
            using var db = _dbContextFactory.CreateDbContext();
            if (tracking)
            {
                return db.UrlMapping.Find(urlMappingId);
            }
            else
            {
                return db.UrlMapping.AsNoTracking().FirstOrDefault(item => item.UrlMappingId == urlMappingId);
            }
        }

        public UrlMapping GetUrlMapping(int siteId, string url)
        {
            using var db = _dbContextFactory.CreateDbContext();
            url = (url.Length > 750) ? url.Substring(0, 750) : url;
            var urlMapping = db.UrlMapping.Where(item => item.SiteId == siteId && item.Url == url).FirstOrDefault();
            if (urlMapping == null)
            {
                var site = _sites.GetSite(siteId);
                if (site.CaptureBrokenUrls)
                {
                    urlMapping = new UrlMapping();
                    urlMapping.SiteId = siteId;
                    urlMapping.Url = url;
                    urlMapping.MappedUrl = "";
                    urlMapping.Requests = 1;
                    urlMapping.CreatedOn = DateTime.UtcNow;
                    urlMapping.RequestedOn = DateTime.UtcNow;
                    urlMapping = AddUrlMapping(urlMapping);
                }
            }
            else
            {
                urlMapping.Requests += 1;
                urlMapping.RequestedOn = DateTime.UtcNow;
                urlMapping = UpdateUrlMapping(urlMapping);
            }
            return urlMapping;
        }

        public void DeleteUrlMapping(int urlMappingId)
        {
            using var db = _dbContextFactory.CreateDbContext();
            UrlMapping urlMapping = db.UrlMapping.Find(urlMappingId);
            db.UrlMapping.Remove(urlMapping);
            db.SaveChanges();
        }
    }
}
