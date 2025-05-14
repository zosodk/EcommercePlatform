﻿using System;
using System.Threading.Tasks;

namespace EcommercePlatform.Application.Interfaces.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
}