using System.Reflection;

namespace MO.Mapper
{
    public static class Mapper
    {
        public static TTarget Map<TSource, TTarget>(TSource source, TTarget? target = null) where TTarget : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // ToList() yerine array kullanın - daha az allocation
            PropertyInfo[] sourceProperties = typeof(TSource)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead)
                .ToArray();

            if (target == null)
            {
                var constructors = typeof(TTarget).GetConstructors(BindingFlags.Instance | BindingFlags.Public);

                // OrderBy yerine tek döngüde parametre sayısına göre bulun
                ConstructorInfo? constructorInfo = null;
                ConstructorInfo? minParamConstructor = null;
                int minParamCount = int.MaxValue;

                foreach (var ctor in constructors)
                {
                    int paramCount = ctor.GetParameters().Length;
                    if (paramCount == 0)
                    {
                        constructorInfo = ctor;
                        break;
                    }
                    if (paramCount < minParamCount)
                    {
                        minParamCount = paramCount;
                        minParamConstructor = ctor;
                    }
                }

                constructorInfo ??= minParamConstructor;

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
                    object?[] constructorParams = new object?[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ParameterInfo param = parameters[i];

                        // LINQ FirstOrDefault yerine for loop
                        PropertyInfo? propertyInfo = null;
                        for (int j = 0; j < sourceProperties.Length; j++)
                        {
                            if (sourceProperties[j].Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                propertyInfo = sourceProperties[j];
                                break;
                            }
                        }

                        if (propertyInfo != null && param.ParameterType.IsAssignableFrom(propertyInfo.PropertyType))
                        {
                            constructorParams[i] = propertyInfo.GetValue(source);
                        }
                        else
                        {
                            constructorParams[i] = param.ParameterType.IsValueType
                                ? Activator.CreateInstance(param.ParameterType)
                                : null;
                        }
                    }
                    target = (TTarget)constructorInfo.Invoke(constructorParams);
                }
            }

            // Target properties alın
            PropertyInfo[] targetProperties = typeof(TTarget)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanWrite)
                .ToArray();

            // Nested loop - küçük property sayıları için Dictionary'den daha hızlı
            foreach (PropertyInfo sourceProp in sourceProperties)
            {
                for (int i = 0; i < targetProperties.Length; i++)
                {
                    PropertyInfo targetProp = targetProperties[i];
                    if (targetProp.Name == sourceProp.Name &&
                        targetProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                    {
                        targetProp.SetValue(target, sourceProp.GetValue(source));
                        break;
                    }
                }
            }

            return target;
        }

        public static List<TTarget> Map<TSource, TTarget>(IEnumerable<TSource> source, IEnumerable<TTarget>? target = null) where TTarget : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var sourceList = source as List<TSource> ?? source.ToList();
            var targetList = target as List<TTarget> ?? target?.ToList() ?? new List<TTarget>();

            // Capacity belirterek allocation'ı azaltın
            List<TTarget> result = new List<TTarget>(sourceList.Count);
            int targetCount = targetList.Count;

            for (int i = 0; i < sourceList.Count; i++)
            {
                TTarget? currentTarget = i < targetCount ? targetList[i] : null;
                result.Add(Map(sourceList[i], currentTarget));
            }

            return result;
        }
    }
}