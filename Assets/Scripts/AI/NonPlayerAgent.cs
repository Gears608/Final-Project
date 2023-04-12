using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NonPlayerAgent : PlayerClass
{
    private int prodPerTick; //the amount of curreny being generated per tick

    [SerializeField]
    private GameObject minePrefab;
    [SerializeField]
    private GameObject barracksPrefab;
    [SerializeField]
    private GameObject towerPrefab;

    [SerializeField]
    private GameObject unitPrefab;

    private int mineCost;
    private int towerCost;
    private int barracksCost;

    [SerializeField]
    float buildRadius;

    [SerializeField]
    private PlayerAgent player;

    private enum ActionType { Barracks, Mine, Tower, Unit }

    [SerializeField]
    private List<Action> buildingPriorities = new List<Action>()
    {
        new Action(ActionType.Mine, 0),
        new Action(ActionType.Barracks, 0),
        new Action(ActionType.Tower, 0),
        new Action(ActionType.Unit, 0)
    };

    private void Start()
    {
        currentBarracks = 0;
        currentMines = 0;
        currentTowers = 0;

        mineCost = minePrefab.GetComponent<Building>().cost;
        barracksCost = barracksPrefab.GetComponent<Building>().cost;
        towerCost = towerPrefab.GetComponent<Building>().cost;

        CalculatePriorities();
    }

    protected void Update()
    {
        if (!worldController.IsGamePaused())
        {
            CurrencySpendTree();
        }
    }

    private void CurrencySpendTree()
    {
        ActionType type = GetBestChoice();
        if (type == ActionType.Unit)
        {
            //unit logic
            Barracks barracks = GetEmptyBarracks();
            if (barracks != null)
            {
                BuyUnit(barracks.transform.position, unitPrefab, barracks);
            }
        }
        else
        {
            if (RemainingBuilds(type))
            {
                Location placementLocation = ChooseLocation(GetBuildingPrefab(type));
                if (placementLocation.valid)
                {
                    if (gold >= GetBuildingCost(type))
                    {
                        PlaceBuilding(type, placementLocation.position);
                        IncreaseBuildingCounter(type);
                    }
                }
            }
        }
    }

    private void CalculatePriorities()
    {
        //sets the priority for a defensive building
        if (currentTowers < maxTowers)
        {
            GetInfo(ActionType.Tower).priority = NearbyAttackersCount() + 1;
        }
        else
        {
            GetInfo(ActionType.Tower).priority = 0;
        }

        if (currentMines < maxMines)
        {
            //sets the priority of a mine/currency generator
            if (prodPerTick == 0)
            {
                GetInfo(ActionType.Mine).priority = float.MaxValue;
            }
            else
            {
                GetInfo(ActionType.Mine).priority = (1f / prodPerTick * 50f) + 1;
            }
        }
        else
        {
            GetInfo(ActionType.Mine).priority = 0;
        }

        if(allUnits.Count < GetMaxUnits())
        {
            GetInfo(ActionType.Unit).priority = Mathf.Max((player.GetUnitCount() * 1.2f) - allUnits.Count, 1);
            GetInfo(ActionType.Barracks).priority = 0;
        }
        else
        {
            //Debug.Log(currentBarracks+" < "+maxBarracks);
            if (currentBarracks < maxBarracks)
            {
                GetInfo(ActionType.Barracks).priority = Mathf.Max((player.GetUnitCount() * 1.2f) - allUnits.Count, 1);
                GetInfo(ActionType.Unit).priority = 0;
            }
            else
            {
                GetInfo(ActionType.Barracks).priority = 0;
            }
        }
    }

    private ActionType GetBestChoice()
    {
        ActionType bestChoice = ActionType.Mine;
        float bestChoiceCost = 0;

        CalculatePriorities();

        foreach (Action building in buildingPriorities)
        {
            //Debug.Log(building.type.ToString() +" : "+building.priority);
            if (building.priority > bestChoiceCost)
            {
                bestChoiceCost = building.priority;
                bestChoice = building.type;
            }
        }

        return bestChoice;
    }

    private Location ChooseLocation(GameObject buildingPrefab)
    {
        Vector2 position = new Vector2();
        bool valid = false;
        //limits the attempts to find a position each frame reduce CPU load
        int attempts = 0;
        while (!valid && attempts < 100)
        {
            position = Random.insideUnitCircle * buildRadius;
            position += (Vector2)transform.position;

            if (worldController.CheckBuildingPlacement(position, buildingPrefab))
            {
                valid = true;
            }

            attempts++;
        }

        return new Location(position, valid);
    }

    private void PlaceBuilding(ActionType type, Vector2 position)
    {
        gold -= GetBuildingCost(type);

        if(type == ActionType.Mine)
        {
            prodPerTick++;
        }

        allBuildings.Add(worldController.PlaceBuilding(position, GetBuildingPrefab(type), this));
    }

    private GameObject GetBuildingPrefab(ActionType type)
    {
        switch (type)
        {
            case ActionType.Mine:
                return minePrefab;
            case ActionType.Barracks:
                return barracksPrefab;
            case ActionType.Tower:
                return towerPrefab;
        }

        return null;
    }

    private void IncreaseBuildingCounter(ActionType type)
    {
        switch (type)
        {
            case ActionType.Mine:
                currentMines++;
                break;
            case ActionType.Barracks:
                currentBarracks++;
                break;
            case ActionType.Tower:
                currentTowers++;
                break;
        }
    }

    private bool RemainingBuilds(ActionType type)
    {
        switch (type)
        {
            case ActionType.Mine:
                return currentMines < maxMines;
            case ActionType.Barracks:
                return currentBarracks < maxBarracks;
            case ActionType.Tower:
                return currentTowers < maxTowers;
        }

        return false;
    }

    private Action GetInfo(ActionType type)
    {
        foreach (Action info in buildingPriorities)
        {
            if (info.type == type)
            {
                return info;
            }
        }

        return null;
    }

    private int GetBuildingCost(ActionType type)
    {
        switch (type)
        {
            case ActionType.Mine:
                return mineCost;
            case ActionType.Barracks:
                return barracksCost;
            case ActionType.Tower:
                return towerCost;
        }

        return -1;
    }

    private int NearbyAttackersCount()
    {
        HashSet<UnitClass> nearbyUnits = new HashSet<UnitClass>();

        foreach (Building building in allBuildings)
        {
            UnitClass[] units = building.GetNearbyEnemies();
            foreach (UnitClass unit in units)
            {
                nearbyUnits.Add(unit);
            }
        }

        return nearbyUnits.Count;
    }

    private int GetMaxUnits()
    {
        int maxUnits = 0;

        foreach(Building building in allBuildings)
        {
            if(building is Barracks)
            {
                Barracks barracks = building.GetComponent<Barracks>();
                maxUnits += barracks.maxUnitCount;
            }
        }
        return maxUnits;
    }

    private void BuyUnit(Vector2 position, GameObject prefab, Barracks home)
    {
        UnitClass unitClass = prefab.GetComponent<UnitClass>();
        if (unitClass.cost <= gold)
        {
            GameObject newUnit = Instantiate(prefab);
            UnitClass newUnitClass = newUnit.GetComponent<UnitClass>();
            allUnits.Add(newUnitClass);
            newUnitClass.owner = this;
            home.AddUnit(newUnitClass);
            gold -= unitClass.cost;
            newUnit.transform.position = position;
        }
    }

    private Barracks GetEmptyBarracks()
    {
        foreach (Building building in allBuildings)
        {
            if (building is Barracks)
            {
                Barracks barracks = building.GetComponent<Barracks>();
                if(barracks.unitCount < barracks.maxUnitCount)
                {
                    return barracks;
                }
            }
        }

        return null;
    }

    /*
     * A function which removes a unit from the units list
     * 
     * GameObject unit - the unit to remove
     */
    public override void RemoveBuilding(Building building)
    {
        if(building is Barracks)
        {
            currentBarracks--;
        }
        else if(building is GoldMine)
        {
            currentMines--;
        }
        else if(building is Tower)
        {
            currentTowers--;
        }

        allBuildings.Remove(building);
    }

    private void UnitManagementTree()
    {

    }

    private class Location
    {
        public Vector2 position;
        public bool valid;

        public Location(Vector2 position, bool valid)
        {
            this.valid = valid;
            this.position = position;
        }
    }

    private class Action 
    {
        public ActionType type;
        public float priority;

        public Action(ActionType type, float priority)
        {
            this.type = type;
            this.priority = priority;
        }
    }

}