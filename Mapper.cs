using System.Reflection;

namespace MO.Mapper
{
    //public static class Mapper
    //{
    //    /// <summary>
    //    /// Maps an instance of one type to an instance of another type.
    //    /// </summary>
    //    /// <typeparam name="TSource">The source type to map from.</typeparam>
    //    /// <typeparam name="TTarget">The target type to map to.</typeparam>
    //    /// <param name="source">The source object to be mapped.</param>
    //    /// <returns>An instance of <typeparamref name="TTarget"/> with properties set from the <paramref name="source"/> object.</returns>
    //    public static TTarget Map<TSource, TTarget>(this TSource source) where TTarget : class
    //    {
    //        // Return null if the source object is null.
    //        if (source == null) return null;

    //        // Retrieve all readable properties from the source type.
    //        var srcProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //            .Where(p => p.CanRead)
    //            .ToList();

    //        // Get the first public constructor of the target type.
    //        ConstructorInfo targetCtor = typeof(TTarget).GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
    //        if (targetCtor == null)
    //        {
    //            // Throw an exception if no public constructor is found.
    //            throw new InvalidOperationException("No public constructor found.");
    //        }

    //        // Get the parameters of the target type's constructor.
    //        var ctorParams = targetCtor.GetParameters();

    //        // If the constructor has no parameters, create an instance of the target type.
    //        if (ctorParams.Length == 0)
    //        {
    //            var target = Activator.CreateInstance<TTarget>();

    //            // Retrieve all writable properties of the target type.
    //            var targetProperties = typeof(TTarget).GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //                .Where(p => p.CanWrite)
    //                .ToDictionary(p => p.Name, p => p);

    //            // Map each source property to the corresponding target property.
    //            foreach (var srcProp in srcProperties)
    //            {
    //                if (targetProperties.TryGetValue(srcProp.Name, out var targetProp))
    //                {
    //                    // Set the target property value if types are compatible.
    //                    if (targetProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
    //                    {
    //                        targetProp.SetValue(target, srcProp.GetValue(source));
    //                    }
    //                }
    //            }
    //            return target;
    //        }
    //        else // If the constructor has parameters, map source properties to constructor parameters.
    //        {
    //            var paramValues = new object[ctorParams.Length];
    //            for (int i = 0; i < ctorParams.Length; i++)
    //            {
    //                var param = ctorParams[i];
    //                var sourceProp = srcProperties.FirstOrDefault(p => p.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
    //                if (sourceProp != null)
    //                {
    //                    // Get the parameter value from the source property.
    //                    paramValues[i] = sourceProp.GetValue(source);
    //                }
    //                else
    //                {
    //                    // Assign default values if the source property is not found.
    //                    paramValues[i] = param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
    //                }
    //            }
    //            // Create the target instance using the constructor parameters.
    //            return (TTarget)targetCtor.Invoke(paramValues);
    //        }
    //    }

    //    /// <summary>
    //    /// Maps a collection of source objects to a collection of target objects.
    //    /// </summary>
    //    /// <typeparam name="TSource">The source type to map from.</typeparam>
    //    /// <typeparam name="TTarget">The target type to map to.</typeparam>
    //    /// <param name="source">The collection of source objects to be mapped.</param>
    //    /// <returns>A list of <typeparamref name="TTarget"/> objects created from the source collection.</returns>
    //    public static List<TTarget> Map<TSource, TTarget>(this IEnumerable<TSource> source) where TTarget : class
    //    {
    //        if (source == null)
    //        {
    //            // Return an empty list if the source collection is null.
    //            return new List<TTarget>();
    //        }

    //        // Map each source object to the target type and return as a list.
    //        return source.Select(s => s.Map<TSource, TTarget>()).ToList();
    //    }
    //}

    public static class Mapper
    {
        /// <summary>
        /// Maps an instance of one type to an instance of another type.
        /// </summary>
        /// <typeparam name="TSource">The source type to map from.</typeparam>
        /// <typeparam name="TTarget">The target type to map to.</typeparam>
        /// <param name="source">The source object to be mapped.</param>
        /// <returns>An instance of <typeparamref name="TTarget"/> with properties set from the <paramref name="source"/> object.</returns>
        public static TTarget Map<TSource, TTarget>(TSource source) where TTarget : class
        {
            // Return null if the source object is null.
            if (source == null) return null;

            // Retrieve all readable properties from the source type.
            var srcProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToList();

            // Get the first public constructor of the target type.
            ConstructorInfo targetCtor = typeof(TTarget).GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
            if (targetCtor == null)
            {
                // Throw an exception if no public constructor is found.
                throw new InvalidOperationException("No public constructor found.");
            }

            // Get the parameters of the target type's constructor.
            var ctorParams = targetCtor.GetParameters();

            // If the constructor has no parameters, create an instance of the target type.
            if (ctorParams.Length == 0)
            {
                var target = Activator.CreateInstance<TTarget>();

                // Retrieve all writable properties of the target type.
                var targetProperties = typeof(TTarget).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .ToDictionary(p => p.Name, p => p);

                // Map each source property to the corresponding target property.
                foreach (var srcProp in srcProperties)
                {
                    if (targetProperties.TryGetValue(srcProp.Name, out var targetProp))
                    {
                        // Set the target property value if types are compatible.
                        if (targetProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                        {
                            targetProp.SetValue(target, srcProp.GetValue(source));
                        }
                    }
                }
                return target;
            }
            else // If the constructor has parameters, map source properties to constructor parameters.
            {
                var paramValues = new object[ctorParams.Length];
                for (int i = 0; i < ctorParams.Length; i++)
                {
                    var param = ctorParams[i];
                    var sourceProp = srcProperties.FirstOrDefault(p => p.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
                    if (sourceProp != null)
                    {
                        // Get the parameter value from the source property.
                        paramValues[i] = sourceProp.GetValue(source);
                    }
                    else
                    {
                        // Assign default values if the source property is not found.
                        paramValues[i] = param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
                    }
                }
                // Create the target instance using the constructor parameters.
                return (TTarget)targetCtor.Invoke(paramValues);
            }
        }

        /// <summary>
        /// Maps a collection of source objects to a collection of target objects.
        /// </summary>
        /// <typeparam name="TSource">The source type to map from.</typeparam>
        /// <typeparam name="TTarget">The target type to map to.</typeparam>
        /// <param name="source">The collection of source objects to be mapped.</param>
        /// <returns>A list of <typeparamref name="TTarget"/> objects created from the source collection.</returns>
        public static List<TTarget> Map<TSource, TTarget>(IEnumerable<TSource> source) where TTarget : class
        {
            if (source == null)
            {
                // Return an empty list if the source collection is null.
                return new List<TTarget>();
            }

            // Map each source object to the target type and return as a list.
            return source.Select(data => Map<TSource, TTarget>(data)).ToList();
        }
    }
}