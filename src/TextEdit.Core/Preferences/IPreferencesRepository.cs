using System.Threading.Tasks;

namespace TextEdit.Core.Preferences
{
    /// <summary>
    /// Abstraction for persisting and loading <see cref="UserPreferences"/>.
    /// Implementations should perform atomic writes and validate the JSON against the preferences schema.
    /// </summary>
    public interface IPreferencesRepository
    {
        /// <summary>
        /// Load preferences; if no persisted preferences exist, return defaults.
        /// </summary>
        Task<UserPreferences> LoadAsync();

        /// <summary>
        /// Save preferences atomically. Implementations should throw on transient I/O errors.
        /// </summary>
        Task SaveAsync(UserPreferences preferences);
    }
}
