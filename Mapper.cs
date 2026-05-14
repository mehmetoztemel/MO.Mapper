using System.Collections.Concurrent;
using System.Reflection;

namespace MO.Mapper
{
    public static class Mapper
    {
        // Tip bazında reflection sonuçlarını cache'de tutuyoruz.
        // Her tip için GetProperties yalnızca ilk çağrıda çalışır,
        // sonraki tüm çağrılarda doğrudan cache'den okunur.
        // ConcurrentDictionary kullandık çünkü ASP.NET Core multi-thread ortamda çalışır.

        // Source tipin okunabilir (CanRead) property'leri
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _readCache = new();

        // Target tipin yazılabilir (CanWrite) property'leri
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _writeCache = new();

        // Target tipin kullanılacak constructor'ı
        private static readonly ConcurrentDictionary<Type, ConstructorInfo?> _ctorCache = new();

        public static TTarget Map<TSource, TTarget>(TSource source, TTarget? target = null) where TTarget : class
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // TSource için okunabilir property'leri al.
            // typeof(TSource) cache'de varsa GetOrAdd fabrika metodunu çalıştırmaz, direkt döner.
            PropertyInfo[] sourceProperties = _readCache.GetOrAdd(
                typeof(TSource),
                t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                      .Where(p => p.CanRead)
                      .ToArray()
            );

            // Dışarıdan hazır bir target nesnesi verilmediyse biz oluşturuyoruz.
            if (target == null)
            {
                // TTarget için uygun constructor'ı cache'den al.
                // ResolveConstructor: önce parametresiz olanı arar, yoksa en az parametreli olanı seçer.
                ConstructorInfo? constructorInfo = _ctorCache.GetOrAdd(typeof(TTarget), ResolveConstructor);

                if (constructorInfo == null)
                    throw new InvalidOperationException("No public constructor found.");

                ParameterInfo[] parameters = constructorInfo.GetParameters();

                if (parameters.Length == 0)
                {
                    // Parametresiz constructor varsa direkt instance oluştur.
                    target = Activator.CreateInstance<TTarget>();
                }
                else
                {
                    // Parametreli constructor için her parametreye karşılık gelen
                    // source property'yi isim eşleşmesiyle bulup değerini geçiyoruz.
                    object?[] constructorParams = new object?[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ParameterInfo param = parameters[i];
                        PropertyInfo? propertyInfo = null;

                        // LINQ yerine for döngüsü: küçük dizilerde daha az allocation
                        for (int j = 0; j < sourceProperties.Length; j++)
                        {
                            if (sourceProperties[j].Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                propertyInfo = sourceProperties[j];
                                break;
                            }
                        }

                        // Eşleşen property bulunduysa ve tipler uyumluysa değerini al,
                        // bulunamadıysa value type için default instance, reference type için null ver.
                        constructorParams[i] = propertyInfo != null && param.ParameterType.IsAssignableFrom(propertyInfo.PropertyType)
                            ? propertyInfo.GetValue(source)
                            : param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
                    }

                    target = (TTarget)constructorInfo.Invoke(constructorParams);
                }
            }

            // TTarget için yazılabilir property'leri cache'den al.
            PropertyInfo[] targetProperties = _writeCache.GetOrAdd(
                typeof(TTarget),
                t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                      .Where(p => p.CanWrite)
                      .ToArray()
            );

            // Source property'leri tek tek dönüp target'ta aynı isimde ve uyumlu tipte
            // olan property'yi bularak değeri aktarıyoruz.
            // Nested loop tercih ettik: az sayıda property için Dictionary'den daha az allocation yapar.
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

        // IEnumerable source için liste dönüşümü.
        // Opsiyonel olarak mevcut bir target listesi de verilebilir;
        // verilirse listedeki mevcut nesneler güncellenir, fazla index'ler için yeni nesne oluşturulur.
        public static List<TTarget> Map<TSource, TTarget>(IEnumerable<TSource> source, IEnumerable<TTarget>? target = null) where TTarget : class
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // Zaten List<T> ise tekrar ToList() çağırıp gereksiz allocation yapmıyoruz.
            var sourceList = source as List<TSource> ?? source.ToList();
            var targetList = target as List<TTarget> ?? target?.ToList() ?? new List<TTarget>();

            // Kapasite önceden verilerek iç dizinin yeniden boyutlandırılması engelleniyor.
            List<TTarget> result = new List<TTarget>(sourceList.Count);
            int targetCount = targetList.Count;

            for (int i = 0; i < sourceList.Count; i++)
            {
                TTarget? currentTarget = i < targetCount ? targetList[i] : null;
                result.Add(Map(sourceList[i], currentTarget));
            }

            return result;
        }

        // Target tipin constructor'ını belirler.
        // Öncelik sırası: parametresiz constructor → en az parametreli constructor.
        // Sonuç cache'e alındığı için bu metot tip başına yalnızca bir kez çalışır.
        private static ConstructorInfo? ResolveConstructor(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            ConstructorInfo? minParamCtor = null;
            int minParamCount = int.MaxValue;

            foreach (var ctor in constructors)
            {
                int paramCount = ctor.GetParameters().Length;

                // Parametresiz bulundu, direkt döndür, aramaya devam etmeye gerek yok.
                if (paramCount == 0)
                    return ctor;

                if (paramCount < minParamCount)
                {
                    minParamCount = paramCount;
                    minParamCtor = ctor;
                }
            }

            // Parametresiz yoksa en az parametreli constructor döner.
            // Hiç public constructor bulunamazsa null döner, çağıran taraf exception fırlatır.
            return minParamCtor;
        }
    }
}
