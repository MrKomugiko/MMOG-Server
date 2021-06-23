using System.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Numerics;
using MMOG;

public class UPDATE_NOTES
{

    public DATA _Data { get; set; }
}

public class DATA
{ 
    public Locations this[LOCATIONS location] {
        get => _Locations[(int)location];
        set => _Locations.Insert((int)location, value);
    }
    public List<Locations> _Locations { get; set; }

    public Items this[ITEMS item] {
        get => _Items[(int)item];
        set => _Items.Insert((int)item, value);
    }
    public List<Items> _Items { get; set; }

}

public class Locations
{
    public Maptypes this[MAPTYPE type] {
        get => _Type[(int)type];
        set => _Type.Insert((int)type, value);
    }
    public string _Name { get; set; }    // Start_First_Floor
    public int _Id { get; set; } // 0
    public Vector3_json _Coordinates { get; set; } // (0,0,0)
    public List<Maptypes> _Type { get; set; }
}

public class Vector3_json
{
    public Vector3_json() {

    }

    public Vector3_json(float x, float y, float z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }

    internal Vector3 ToVector3() => new Vector3(x,y,z);
    
}

public class Maptypes : IDataElement
{
    public int _Id { get; set; }
    public string _Name { get ; set ; }
    public string _Type { get; set; } // _ObstacleMAP_Version
    public int _Version { get; set; }// 1000

    public void UpdateVersionNumber() {
        this._Version++;
    }
}

public class Items : IDataElement
{
    public Items() {
    }

    public Items(int id, string name, string type) {
        _Id = id;
        _Name = name;
        _Type = type;

        Console.WriteLine($"dodano {_Name}, wersja {_Version}");
    }

    public int _Id { get; set; }
    public string _Name { get; set; }
    public string _Type { get; set; }
    public int _Version { get; set; } = 1000;

    public void UpdateVersionNumber() {
       this._Version++;
    }
}


public interface IDataElement
{
    public int _Id { get; set; }
    public string _Name { get; set; }
    public int _Version { get; set; }

    public void UpdateVersionNumber();
    
}

