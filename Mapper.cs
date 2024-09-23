using System.Reflection;

namespace MO.Mapper
{
    public static class Mapper
    {
        public static TTarget Map<TSource, TTarget>(TSource source, TTarget target = null) where TTarget : class
        {
            if (source == null)
            {
                return null;
            }

            List<PropertyInfo> sourceProperties = (from p in typeof(TSource).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                                   where p.CanRead
                                                   select p).ToList();

            if (target == null)
            {
                ConstructorInfo constructorInfo = typeof(TTarget).GetConstructors(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();
                if (constructorInfo == null)
                {
                    throw new InvalidOperationException("No public constructor found.");
                }

                ParameterInfo[] parameters = constructorInfo.GetParameters();
                if (parameters.Length == 0)
                {
                    target = Activator.CreateInstance<TTarget>();
                }
                else
                {
                    object[] constructorParams = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ParameterInfo param = parameters[i];
                        PropertyInfo propertyInfo = sourceProperties.FirstOrDefault((PropertyInfo p) => p.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
                        if (propertyInfo != null)
                        {
                            constructorParams[i] = propertyInfo.GetValue(source);
                        }
                        else
                        {
                            constructorParams[i] = (param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null);
                        }
                    }
                    target = (TTarget)constructorInfo.Invoke(constructorParams);
                }
            }

            Dictionary<string, PropertyInfo> targetProperties = (from p in typeof(TTarget).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                                                 where p.CanWrite
                                                                 select p).ToDictionary((PropertyInfo p) => p.Name, (PropertyInfo p) => p);

            foreach (PropertyInfo sourceProp in sourceProperties)
            {
                if (targetProperties.TryGetValue(sourceProp.Name, out var targetProp) && targetProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                {
                    targetProp.SetValue(target, sourceProp.GetValue(source));
                }
            }

            return target;
        }

        public static List<TTarget> Map<TSource, TTarget>(IEnumerable<TSource> source, IEnumerable<TTarget> target = null) where TTarget : class
        {
            if (source == null)
            {
                return new List<TTarget>();
            }

            List<TTarget> result = new List<TTarget>();
            var targetList = target?.ToList() ?? new List<TTarget>();
            var targetCount = targetList.Count;

            for (int i = 0; i < source.Count(); i++)
            {
                TSource currentSource = source.ElementAt(i);
                TTarget currentTarget = i < targetCount ? targetList[i] : null;
                result.Add(Map(currentSource, currentTarget));
            }

            return result;
        }
    }

}