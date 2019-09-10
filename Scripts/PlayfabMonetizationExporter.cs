﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_PURCHASING && UNITY_EDITOR
using UnityEngine.Purchasing;
#endif
using LitJson;

public class PlayfabMonetizationExporter : MonoBehaviour
{
    public GameInstance gameInstance;
    public MonetizationManager monetizationManager;

    [Header("Catalog")]
    public string exportingCatalogVersion = "Catalog-1";

#if UNITY_EDITOR
    [ContextMenu("Export Catalog")]
    public void ExportCatalog()
    {
        var allItems = gameInstance.GetAllItems();
        var allBundles = monetizationManager.products;
        var catalogItems = new Dictionary<string, CatalogItem>();
        foreach (var item in allItems)
        {
            var virtualCurrencyPrices = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(item.price.id))
                virtualCurrencyPrices[item.price.id] = item.price.amount;
            foreach (var price in item.prices)
            {
                if (!string.IsNullOrEmpty(price.id))
                    virtualCurrencyPrices[price.id] = price.amount;
            }
            catalogItems[item.GetId()] = new CatalogItem()
            {
                ItemId = item.GetId(),
                CatalogVersion = exportingCatalogVersion,
                DisplayName = item.GetTitle(),
                Description = item.GetDescription(),
                VirtualCurrencyPrices = virtualCurrencyPrices,
                Consumable = new CatalogConsumable(),
                Bundle = new CatalogBundle(),
            };
        }

        var iapCatalog = ProductCatalog.LoadDefaultCatalog();
        var iapCatalogDict = new Dictionary<string, ProductCatalogItem>();
        foreach (var product in iapCatalog.allProducts)
        {
            iapCatalogDict[product.id] = product;
        }

        ProductCatalogItem tempIAPItem;
        foreach (var bundle in allBundles)
        {
            var virtualCurrencyPrices = new Dictionary<string, int>();
            virtualCurrencyPrices.Add("RM", 0); // TODO: SET USD PRICE
            var itemIds = new List<string>();
            foreach (var item in bundle.items)
            {
                itemIds.Add(item.GetId());
            }
            var rewardCurrencies = new Dictionary<string, int>();
            foreach (var currency in bundle.currencies)
            {
                rewardCurrencies[currency.id] = currency.amount;
            }

            if (iapCatalogDict.TryGetValue(bundle.GetId(), out tempIAPItem))
            {
                // Google play
                catalogItems[tempIAPItem.GetStoreID(GooglePlay.Name)] = new CatalogItem()
                {
                    ItemId = tempIAPItem.GetStoreID(GooglePlay.Name),
                    CatalogVersion = exportingCatalogVersion,
                    DisplayName = bundle.GetTitle(),
                    Description = bundle.GetDescription(),
                    VirtualCurrencyPrices = virtualCurrencyPrices,
                    Consumable = new CatalogConsumable(),
                    Bundle = new CatalogBundle()
                    {
                        BundledItems = itemIds,
                        BundledVirtualCurrencies = rewardCurrencies,
                    }
                };
                // Apple appstore
                catalogItems[tempIAPItem.GetStoreID(AppleAppStore.Name)] = new CatalogItem()
                {
                    ItemId = tempIAPItem.GetStoreID(AppleAppStore.Name),
                    CatalogVersion = exportingCatalogVersion,
                    DisplayName = bundle.GetTitle(),
                    Description = bundle.GetDescription(),
                    VirtualCurrencyPrices = virtualCurrencyPrices,
                    Consumable = new CatalogConsumable(),
                    Bundle = new CatalogBundle()
                    {
                        BundledItems = itemIds,
                        BundledVirtualCurrencies = rewardCurrencies,
                    }
                };
            }
        }

        var dropTables = new List<DropTableItem>();

        var catalog = new PlayfabCatalog()
        {
            CatalogVersion = exportingCatalogVersion,
            Catalog = new List<CatalogItem>(catalogItems.Values),
            DropTables = dropTables,
        };
        var json = JsonMapper.ToJson(catalog);
        var path = EditorUtility.SaveFilePanel("Export Catalog", Application.dataPath, "CATALOG", "json");
        if (path.Length > 0)
            File.WriteAllText(path, json);
    }

    [ContextMenu("Export Currencies")]
    public void ExportCurrencies()
    {
        var currencies = new List<PlayfabCurrency>();
        foreach (var currency in monetizationManager.currencies)
        {
            var code = currency.id;
            var name = currency.name;
            var startAmount = currency.startAmount;
            currencies.Add(new PlayfabCurrency()
            {
                CurrencyCode = code,
                DisplayName = name,
                InitialDeposit = startAmount,
                RechargeRate = 0,
                RechargeMax = 0,
                CurrencyCodeFull = code + " (" + name + ")",
            });
        }
        var json = JsonMapper.ToJson(currencies);
        var path = EditorUtility.SaveFilePanel("Export Currencies", Application.dataPath, "CURRENCIES", "json");
        if (path.Length > 0)
            File.WriteAllText(path, json);
    }
#endif

    [System.Serializable]
    public class PlayfabCurrency
    {
        public string CurrencyCode;     // GE
        public string DisplayName;      // Gem
        public int InitialDeposit;      // 0
        public int RechargeRate;        // 0
        public int RechargeMax;         // 0
        public string CurrencyCodeFull; // GE (Gem)
        public string GameManagerClassMetadata;
    }

    [System.Serializable]
    public class PlayfabCatalog
    {
        public string CatalogVersion;
        public List<CatalogItem> Catalog;
        public List<DropTableItem> DropTables;
    }

    [System.Serializable]
    public class CatalogItem
    {
        public string ItemId;
        public string ItemClass;
        public string CatalogVersion;
        public string DisplayName;
        public string Description;
        public Dictionary<string, int> VirtualCurrencyPrices;
        public Dictionary<string, int> RealCurrencyPrices;
        public List<string> Tags;
        public object CustomData;
        public CatalogConsumable Consumable;
        public CatalogContainer Container;
        public CatalogBundle Bundle;
        public bool CanBecomeCharacter;
        public bool IsStackable;
        public bool IsTradable;
        public string ItemImageUrl;
        public bool IsLimitedEdition;
        public int InitialLimitedEditionCount;
        public object ActivatedMembership;
    }

    [System.Serializable]
    public class CatalogConsumable
    {
        public int? UsageCount;
        public int? UsagePeriod;
        public string UsagePeriodGroup;
    }

    [System.Serializable]
    public class CatalogContainer
    {
        public string KeyItemId;
        public List<string> ItemContents;
        public List<string> ResultTableContents; // Drop tables
        public Dictionary<string, int> VirtualCurrencyContents;
    }

    [System.Serializable]
    public class CatalogBundle
    {
        public List<string> BundledItems;
        public List<string> BundledResultTables; // Drop tables
        public Dictionary<string, int> BundledVirtualCurrencies;
    }

    [System.Serializable]
    public class DropTableItem
    {
        public string TableId;
        public List<DropTableItemNode> Nodes;
    }

    [System.Serializable]
    public class DropTableItemNode
    {
        public string ResultItemType = "ItemId";
        public string ResultItem;
        public int Weight;
    }
}
