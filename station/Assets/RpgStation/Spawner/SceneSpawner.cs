﻿using System;
using System.Collections;
using System.Collections.Generic;
using Malee;
using UnityEngine;
using Weighted_Randomizer;
using Object = UnityEngine.Object;

namespace Station
{
    public class SceneSpawner : MonoBehaviour
    {
        public InitMode InitMode;
        public string SpawnId;
        public int SpawnAmount;
        public EntitiesSelectionMode entitiesSelectionMode;
        public ReSpawnMode ReSpawnMode;
        [Reorderable] public SpawnDataList DataList = new SpawnDataList();

        //cached
        private SavingSystem _savingSystem;
        private StationMechanics _mechanics;

        public void Init(StationMechanics stationMechanics)
        {
            _mechanics = stationMechanics;
            if (InitMode == InitMode.SAVED)
            {
                _savingSystem = RpgStation.GetSystemStatic<SavingSystem>();
               
                var spawnerSave = _savingSystem.GetModule<SpawnerSave>();
                var spawnStateMap = spawnerSave.GetDataById(SpawnId)?.SpawnsStateMap;
                if (spawnStateMap == null || spawnStateMap.Count == 0)
                {
                    //init it
                    if (entitiesSelectionMode == EntitiesSelectionMode.EACH)
                    {
                        foreach (var spawnableData in DataList)
                        {
                            spawnableData.SpawnEntity(_mechanics);
                            spawnerSave.AddEntry(SpawnId, spawnableData.Id, "todo_data");
                        }
                    }
                    else if (entitiesSelectionMode == EntitiesSelectionMode.RANDOM_FROM_AMOUNT)
                    {
                    }

                    spawnerSave.Save();
                }
                else
                {
                
                    foreach (var spawnedEntries in spawnStateMap)
                    {
                    
                        // spawnedEntries.Value
                    }
                }
            }
            else if(InitMode == InitMode.ALWAYS)
            {
                if (entitiesSelectionMode == EntitiesSelectionMode.EACH)
                {
                    foreach (var spawnableData in DataList)
                    {
                        spawnableData.SpawnEntity(_mechanics);
                    }
                }
                else if (entitiesSelectionMode == EntitiesSelectionMode.RANDOM_FROM_AMOUNT)
                {
                    //selects
                    var randomizer = DataList.GetRandomizer();
                    for (int i = 0; i < SpawnAmount; i++)
                    {
                        var data = randomizer.NextWithReplacement();
                        data.SpawnEntity(_mechanics);
                    }
                }

            }

          

        }

        public SpawnData GetDataById(string id)
        {
            foreach (var entry in DataList)
            {
                if (entry.Id == id) return entry;
            }

            return null;
        }
    }

    [Serializable]
    public class SpawnDataList : ReorderableArray<SpawnData>
    {
        private StaticWeightedRandomizer<SpawnData> _weightedRandomizer = null;
        
        public  StaticWeightedRandomizer<SpawnData> GetRandomizer()
        {
            if (_weightedRandomizer == null)
            {
                _weightedRandomizer = new StaticWeightedRandomizer<SpawnData>();
                foreach (var data in ToArray())
                {
                    _weightedRandomizer.Add(data,data.Weight);
                }
            }

           
            return _weightedRandomizer;
        }
    }
    
   
    
    [Serializable]
    public class SpawnData : IComparable<SpawnData>
    {
        public string Id;
        public SpawnObjectType SpawnType;
        public string ObjectId;
        public GameObject Prefab;
        public PositionProvider Position;
        public bool Unique;
        public int Weight;
        public string SaveId;
        public int CompareTo(SpawnData other)
        {
            return string.Compare(Id, other.Id, StringComparison.Ordinal);
        }

        public void SpawnEntity(StationMechanics mechanics)
        {
            if (Position == null)
            {
                Debug.LogError("missing position provider: "+Id);
                return;
            }

            Position.Generate();

            switch (SpawnType)
            {
                case SpawnObjectType.NPC:
                    var dbSystem = RpgStation.GetSystemStatic<DbSystem>();
                    var npcDb = dbSystem.GetDb<NpcDb>();
                    var npcMeta = npcDb.GetEntry(ObjectId);
                    if (npcMeta == null)
                    {
                        return;
                    }
                    BaseCharacterData baseData = new BaseCharacterData();
                    baseData.Gender = "male";
                    baseData.Identifier = ObjectId;
                    baseData.Position = Position.GetPosition();
                    baseData.Rotation = Position.GetRotation();
                    baseData.RaceId = npcMeta.RaceId;
                    baseData.CharacterType = new NpcCharacterType();
                    List<object> data = new List<object>();
                    data.Add(npcMeta);
                    InstantiateCharacterTask npcTask = new InstantiateCharacterTask(npcMeta.PrefabId, baseData, data.ToArray(), mechanics);
                    npcTask.Execute();
                    break;
                case SpawnObjectType.ITEM:

                    break;
                case SpawnObjectType.PREFAB:
                    Object.Instantiate(Prefab, Position.GetPosition(), Quaternion.Euler(Position.GetRotation()));
                    break;
            }
        }
    }

    public enum SpawnObjectType
    {
        NPC,
        ITEM,
        PREFAB
    }

    public enum InitMode
    {
        SAVED,
        ALWAYS
    }

    public enum EntitiesSelectionMode
    {
        EACH,
        RANDOM_FROM_AMOUNT
    }

    public enum ReSpawnMode
    {
        NONE,
        TIMER,
        REACH_POPULATION_THRESHOLD
    }
}
