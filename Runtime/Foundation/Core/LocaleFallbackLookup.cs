using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>One entry lookup in the table of one locale, for <see cref="LocaleFallbackLookup"/>.</summary>
    /// <typeparam name="TLocale">Whatever identifies a locale (a Unity <c>Locale</c>, a code).</typeparam>
    /// <param name="locale">The locale whose table is read.</param>
    /// <param name="text">The text when the result is <see cref="LookupStatus.Found"/>, otherwise null.</param>
    public delegate LookupStatus LocaleEntryLookup<TLocale>(TLocale locale, out string text);

    /// <summary>
    /// Looks an entry up in the selected locale and then along its fallback chain (es-AR, then es, then en), the order Unity Localization
    /// uses: the selected locale first, then breadth first through the fallbacks each locale declares, every locale at most once, so a
    /// chain that loops back on itself ends. A locale whose entry is missing, empty or could not be formatted (the lookup answers
    /// <see cref="LookupStatus.EntryMissing"/>) passes the key on to the next one. Only the selected locale's
    /// <see cref="LookupStatus.TableMissing"/> is reported as such; a fallback locale whose table is not there is skipped.
    /// Pure C#; the Unity Localization backend supplies the locales and the table reads.
    /// </summary>
    public static class LocaleFallbackLookup
    {
        /// <summary>The most locales one lookup visits, the selected one included: a guard against a chain that never ends.</summary>
        public const int MaxLocales = 16;

        /// <summary>
        /// The text of the first locale of the chain that has it. Returns <see cref="LookupStatus.Found"/> with the text,
        /// <see cref="LookupStatus.TableMissing"/> when the selected locale's table is missing (the fallbacks are not asked), or
        /// <see cref="LookupStatus.EntryMissing"/> when no locale of the chain has the entry. Exceptions of <paramref name="fallbacksOf"/>
        /// end the walk (the entry is then missing); exceptions of <paramref name="lookup"/> are the caller's.
        /// </summary>
        /// <typeparam name="TLocale">Whatever identifies a locale; compared with its own equality.</typeparam>
        /// <param name="selected">The selected locale. Null asks <paramref name="lookup"/> once, for null, and follows no fallbacks.</param>
        /// <param name="fallbacksOf">The fallbacks a locale declares, in priority order; may return null. Null follows no fallbacks.</param>
        /// <param name="lookup">Reads the entry in one locale's table.</param>
        /// <param name="text">The text when the result is <see cref="LookupStatus.Found"/>, otherwise null.</param>
        public static LookupStatus Find<TLocale>(TLocale selected, Func<TLocale, IEnumerable<TLocale>> fallbacksOf, LocaleEntryLookup<TLocale> lookup, out string text)
            where TLocale : class
        {
            text = null;
            if (lookup == null)
                return LookupStatus.EntryMissing;

            LookupStatus status = lookup(selected, out text);
            if (status == LookupStatus.Found && text != null)
                return LookupStatus.Found;
            text = null;
            if (status == LookupStatus.TableMissing)
                return LookupStatus.TableMissing;
            if (selected == null || fallbacksOf == null)
                return LookupStatus.EntryMissing;

            HashSet<TLocale> visited = new HashSet<TLocale> { selected };
            Queue<TLocale> queue = new Queue<TLocale>();
            if (!Enqueue(selected, fallbacksOf, visited, queue))
                return LookupStatus.EntryMissing;

            while (queue.Count > 0)
            {
                TLocale locale = queue.Dequeue();
                string found;
                if (lookup(locale, out found) == LookupStatus.Found && found != null)
                {
                    text = found;
                    return LookupStatus.Found;
                }
                if (!Enqueue(locale, fallbacksOf, visited, queue))
                    break;
            }
            return LookupStatus.EntryMissing;
        }

        // Queues the fallbacks of locale that were not seen yet. False when the walk must end (the fallbacks threw).
        static bool Enqueue<TLocale>(TLocale locale, Func<TLocale, IEnumerable<TLocale>> fallbacksOf, HashSet<TLocale> visited, Queue<TLocale> queue)
            where TLocale : class
        {
            IEnumerable<TLocale> fallbacks;
            try
            {
                fallbacks = fallbacksOf(locale);
                if (fallbacks == null)
                    return true;
                foreach (TLocale fallback in fallbacks)
                {
                    if (visited.Count >= MaxLocales)
                        return true;
                    if (fallback != null && visited.Add(fallback))
                        queue.Enqueue(fallback);
                }
            }
            catch (Exception ex)
            {
                UiLog.Warning(ex);
                return false;
            }
            return true;
        }
    }
}
