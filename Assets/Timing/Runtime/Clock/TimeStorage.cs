using UnityEngine;

namespace Timing.Clock
{
    public interface ITimeStorage
    {
        void Save(string key, string json);
        bool TryLoad(string key, out string json);
    }

    public sealed class PlayerPrefsTimeStorage : ITimeStorage
    {
        public void Save(string key, string json) => PlayerPrefs.SetString(key, json);
        public bool TryLoad(string key, out string json)
        {
            if (!PlayerPrefs.HasKey(key)) { json = null; return false; }
            json = PlayerPrefs.GetString(key);
            return true;
        }
    }
}
