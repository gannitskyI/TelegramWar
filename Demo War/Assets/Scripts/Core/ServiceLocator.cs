using System.Collections.Generic;
using System;
using UnityEngine;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>(32);
    private static readonly Dictionary<Type, WeakReference> weakServices = new Dictionary<Type, WeakReference>(16);

    public static void Register<T>(T service) where T : class
    {
        if (service == null)
        {
            Debug.LogError("Нельзя зарегистрировать null сервис");
            return;
        }
        services[typeof(T)] = service;
    }

    public static void Register(Type type, object service)
    {
        if (type == null || service == null)
        {
            Debug.LogError("Нельзя зарегистрировать null тип или сервис");
            return;
        }
        services[type] = service;
    }

    public static T Get<T>() where T : class
    {
        return services.TryGetValue(typeof(T), out var service) ? service as T : null;
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        if (services.TryGetValue(typeof(T), out var serviceObj))
        {
            service = serviceObj as T;
            return service != null;
        }
        service = null;
        return false;
    }

    public static void RegisterWeak<T>(T service) where T : class
    {
        if (service == null)
        {
            Debug.LogError("Нельзя зарегистрировать null слабый сервис");
            return;
        }
        weakServices[typeof(T)] = new WeakReference(service);
    }

    public static bool TryGetWeak<T>(out T service) where T : class
    {
        if (weakServices.TryGetValue(typeof(T), out var weakRef) && weakRef.IsAlive)
        {
            service = weakRef.Target as T;
            return service != null;
        }
        service = null;
        return false;
    }

    public static bool IsRegistered<T>() where T : class => services.ContainsKey(typeof(T));
    public static bool IsRegistered(Type type) => services.ContainsKey(type);

    public static void Unregister<T>() where T : class
    {
        var type = typeof(T);
        services.Remove(type);
        weakServices.Remove(type);
    }

    public static void Clear()
    {
        services.Clear();
        weakServices.Clear();
    }

    public static void CleanupWeakReferences()
    {
        var toRemove = new List<Type>();
        foreach (var kvp in weakServices)
        {
            if (!kvp.Value.IsAlive)
                toRemove.Add(kvp.Key);
        }

        foreach (var type in toRemove)
            weakServices.Remove(type);
    }
}