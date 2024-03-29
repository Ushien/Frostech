using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [SerializeField] private int _width, _height;

    [SerializeField] private float gap_between_tiles = 0.05f;
    [SerializeField] private Tile _tilePrefab;
    [SerializeField] private Transform _cam;

    private Dictionary<Vector2, Tile> _tiles;
    private enum Selection_mode {
        Single_selection,
        Horizontal_selection,
        Vertical_selection,
        All,
        Special
    }
    private Selection_mode selection_mode = Selection_mode.Single_selection;
    private Tile main_selection;    
    private List<Tile> selected_tiles;

    void Awake(){
        Instance = this;
    }

    public void GenerateGrid() {
        _tiles = new Dictionary<Vector2, Tile>();
        for (int x = 0; x < _width; x++) {
            for (int y = 0; y <_height; y++) {

                var spawnedTile = Instantiate(_tilePrefab, new Vector3(gap_between_tiles*x + x, gap_between_tiles*y + y), Quaternion.identity);

                spawnedTile.name = $"Tile {x} {y}";
                spawnedTile.x_position = x;
                spawnedTile.y_position = y;

                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                spawnedTile.Init(isOffset);

                _tiles[new Vector2(x, y)] = spawnedTile;
            }
        }
        _cam.transform.position = new Vector3((float)_width/2, (float)_height/2, -10);
    }

    public Tile GetTileAtPosition(Vector2 pos){
        if (_tiles.TryGetValue(pos, out var tile)) {
            return tile;
        }

        return null;
    }

    private List<Tile> ReturnTilesList(int width = -1, int height = -1, bool occupiedByUnit = false){
        List<Tile> tiles_list = new List<Tile>();
        int compteur = 0;

        for (int i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                if((width == -1 || width == i)&&(height == -1 || height == j)){
                    if(occupiedByUnit){
                        //TODO
                    }
                    tiles_list.Add(GetTileAtPosition(new Vector2(i, j)));
                    compteur++;
                }
            }
        }
        return tiles_list;
    }

    private List<BaseUnit> UnitsFromTiles(List<Tile> tiles){
        List<BaseUnit> units_list = new List<BaseUnit>();
        foreach (Tile tile in tiles){
            if(tile.OccupiedUnit != null){
                units_list.Add(tile.OccupiedUnit);
            }
        }
        return units_list;
    }

    void Update(){

        main_selection = null;

        foreach (Tile tile in ReturnTilesList()){
            bool selected = tile.main_selection;
            if(selected){
                main_selection = tile;
            }
            tile._highlight.SetActive(false);
        }

        if(main_selection != null){

            selected_tiles = new List<Tile>();
            switch(selection_mode){

                case Selection_mode.Single_selection:

                    selected_tiles = ReturnTilesList(width : main_selection.x_position, height : main_selection.y_position);
                    break;

                case Selection_mode.Horizontal_selection:

                    selected_tiles = ReturnTilesList(height : main_selection.y_position);
                    break;

                case Selection_mode.Vertical_selection:

                    selected_tiles = ReturnTilesList(width : main_selection.x_position);
                    break;

                case Selection_mode.All:

                    selected_tiles = ReturnTilesList();
                    break;

                case Selection_mode.Special:

                    //TODO
                    break;
                    
            }

            foreach (Tile tile in selected_tiles){
                tile._highlight.SetActive(true);
            }
        }

        if (Input.GetMouseButtonDown(0)){

            var units_list = UnitsFromTiles(selected_tiles);
            if(units_list != null){
                foreach( var x in units_list) {
                    DumpToConsole(x);
                    //Debug.Log( x.GetComponent<Enemy1>().ToString());
                    //Debug.Log( x.ToString());
                }
            }
            
        }

        if (Input.GetMouseButtonDown(1)){
            
            CycleTroughSelectionModes();
            
        }

    }

    public Tile GetRandomTile() {
        //TODO Fix this function
        return _tiles.OrderBy(t => Random.value).First().Value;
    }

    public static void DumpToConsole(object obj)
    {
        var output = JsonUtility.ToJson(obj, true);
        Debug.Log(output);
    }

    public void CycleTroughSelectionModes()
    {
        /*
        bool next_mode = false;

        foreach (var mode in System.Enum.GetNames(typeof(Selection_mode)))
        {
            if(mode == selection_mode){
                next_mode = true;
            }
            if(next_mode){
                selection_mode = mode;
                next_mode = false;
            }
        }
        if (next_mode){
            selection_mode = Selection_mode[0];
        }
*/
        //Debug.Log(selection_mode);

        if (selection_mode == Selection_mode.Single_selection){
            selection_mode = Selection_mode.Vertical_selection;
        }
        else{
            selection_mode = Selection_mode.Single_selection;
        }
    }
}