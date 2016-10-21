using System.Collections.Generic;

namespace Nuve.DataStore
{
    /// <summary>
    /// DataStore metodlarını profile etmek için kullanılır.
    /// </summary>
    public interface IDataStoreProfiler
    {
        /// <summary>
        /// Profile başlangıcında bu metod çağrılır. Çıktı olarak verilen obje <see cref="Finish"/> metodunda parametre olarak dönecektir. 
        /// </summary>
        /// <param name="method">Çalıştırılan metodun sınıf ismi ile beraber uzun ismi</param>
        /// <param name="key">Metoda yollanan key</param>
        /// <returns>Çıkış olarak verilen objeye göre profile bilgileri gruplanır. <see cref="Finish"/> metodunda bu obje parametre olarak gelir.</returns>
        object Begin(string method, string key);

        /// <summary>
        /// Profile bitişinde bu metod çağrılır.
        /// </summary>
        /// <param name="context"><see cref="Begin"/> metodunda çıktı olarak verilen obje</param>
        /// <param name="results">Profile sonucu</param>
        /// <returns></returns>
        void Finish(object context, params DataStoreProfileResult[] results);

        object GetContext();
    }
}