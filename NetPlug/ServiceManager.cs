using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace netplug
{

    public interface IServiceManager
    {
        void registerService(String name, Object service);
        void unregisterService(Object service);
        T getService<T>(String name);
        List<T> getServices<T>();
        void unregisterAllServices();
        void startServices();
        void stopServices();
    }

    public class ServiceManager : IServiceManager
    {
        private Dictionary<string, Object> services = new Dictionary<string, object>();

        private static ServiceManager instance;

        public static T get<T>(string name)
        {
            if (null == instance)
            {
                return default(T);
            }

            return instance.getService<T>(name);
        }

        public static IServiceManager Instance()
        {
            return instance;
        }

        public ServiceManager()
        {
            instance = this;
        }

        public void registerService(string name, object service)
        {
            lock (((IDictionary)services).SyncRoot)
            {
                services[name] = service;
            }
        }

        public void unregisterService(object service)
        {
            List<string> removeKeys = new List<string>();

            lock (((IDictionary)services).SyncRoot)
            {
                foreach (KeyValuePair<string, Object> pair in services)
                {
                    if (pair.Value == service)
                    {
                        removeKeys.Add(pair.Key);
                    }
                }

                foreach (string removeKey in removeKeys)
                {
                    services.Remove(removeKey);
                }
            }
        }

        public T getService<T>(string name)
        {
            Object obj = null;
            if (services.TryGetValue(name, out obj))
            {
                if (obj is T)
                {
                    return (T)obj;
                }
            }

            return default(T);
        }

        public void startServices()
        {
            lock (((IDictionary)services).SyncRoot)
            {
                foreach (Object serviceObj in services.Values)
                {
                    if (serviceObj is IService)
                    {
                        ((IService)serviceObj).start();
                    }
                }
            }
        }

        public void stopServices()
        {
            lock (((IDictionary)services).SyncRoot)
            {
                // unfortunately, Dictionary.Values.Reverse exists requires .Net 4.5

                Dictionary<string, object>.ValueCollection objs = services.Values;

                Stack<IService> rev_services = new Stack<IService>();

                foreach (Object serviceObj in objs)
                {
                    if (serviceObj is IService)
                    {
                        rev_services.Push((IService)serviceObj);
                    }
                }

                foreach (IService service in rev_services)
                {
                    service.stop();
                }
            }
        }

        public List<T> getServices<T>()
        {
            List<T> serviceList = new List<T>();

            lock (((IDictionary)services).SyncRoot)
            {
                foreach (Object serviceObj in services.Values)
                {
                    if (serviceObj is T)
                    {
                        serviceList.Add((T)serviceObj);
                    }
                }
            }

            return serviceList;
        }

        public void unregisterAllServices()
        {
            lock (((IDictionary)services).SyncRoot)
            {
                services.Clear();
            }
        }
    }
}
