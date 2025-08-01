using System.Threading.Tasks;
using UnityEngine;

public class AddressableUpgradeLoader
{
    private static AddressableUpgradeLoader instance;
    public static AddressableUpgradeLoader Instance
    {
        get
        {
            if (instance == null)
                instance = new AddressableUpgradeLoader();
            return instance;
        }
    }

    private UpgradeDatabase cachedDatabase;
    private bool isLoaded = false;

    public async Task<UpgradeDatabase> LoadUpgradeDatabaseAsync()
    {
        if (isLoaded && cachedDatabase != null)
            return cachedDatabase;

        var addressableManager = ServiceLocator.Get<AddressableManager>();
        if (addressableManager == null)
        {
            Debug.LogError("AddressableManager not found! Cannot load UpgradeDatabase.");
            return null;
        }

        try
        {
            Debug.Log("Loading UpgradeDatabase from Addressables with key 'UpgradeDatabase'...");
            cachedDatabase = await addressableManager.LoadAssetAsync<UpgradeDatabase>("UpgradeDatabase");

            if (cachedDatabase == null)
            {
                Debug.LogError("UpgradeDatabase not found in Addressables!");
                Debug.LogError("SOLUTION:");
                Debug.LogError("1. Create UpgradeDatabase asset: Create -> Game -> Upgrade Database");
                Debug.LogError("2. Add your UpgradeConfig assets to the 'All Upgrades' list");
                Debug.LogError("3. Mark the UpgradeDatabase as Addressable with key 'UpgradeDatabase'");
                return null;
            }

            cachedDatabase.Initialize();

            if (cachedDatabase.AllUpgrades == null || cachedDatabase.AllUpgrades.Count == 0)
            {
                Debug.LogError("UpgradeDatabase found but is EMPTY!");
                Debug.LogError("Please add your UpgradeConfig assets to the database's 'All Upgrades' list");
                return null;
            }

            Debug.Log($"Successfully loaded UpgradeDatabase with {cachedDatabase.AllUpgrades.Count} upgrades:");
            foreach (var upgrade in cachedDatabase.AllUpgrades)
            {
                if (upgrade != null)
                {
                    Debug.Log($"- {upgrade.DisplayName} (ID: {upgrade.UpgradeId}, Type: {upgrade.Type})");
                }
                else
                {
                    Debug.LogWarning("Found null upgrade in database!");
                }
            }

            isLoaded = true;
            return cachedDatabase;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception while loading UpgradeDatabase: {e.Message}");
            return null;
        }
    }

    public void ClearCache()
    {
        cachedDatabase = null;
        isLoaded = false;
        Debug.Log("UpgradeDatabase cache cleared");
    }

    public bool IsLoaded() => isLoaded && cachedDatabase != null;

    public int GetLoadedUpgradeCount() => cachedDatabase?.AllUpgrades?.Count ?? 0;
}