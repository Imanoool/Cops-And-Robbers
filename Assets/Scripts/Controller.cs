using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Controller : MonoBehaviour
{
    // Referencias a objetos de Unity
    public GameObject board;              // Objeto que contiene el tablero
    public GameObject[] cops = new GameObject[2];  // Array de los 2 policías
    public GameObject robber;             // Objeto del ladrón
    public Text rounds;                   // Texto para mostrar el número de rondas
    public Text finalMessage;             // Mensaje de fin de juego
    public Button playAgainButton;        // Botón para reiniciar el juego

    // Variables de estado del juego
    Tile[] tiles = new Tile[Constants.NumTiles];  // Array de todas las casillas
    private int roundCount = 0;           // Contador de rondas
    private int state;                    // Estado actual del juego
    private int clickedTile = -1;         // Casilla clickeada (-1 = ninguna)
    private int clickedCop = 0;           // Índice del policía seleccionado

    /// <summary>
    /// Inicialización del juego al empezar
    /// </summary>
    void Start()
    {
        InitTiles();          // Configura las casillas del tablero
        InitAdjacencyLists(); // Establece las conexiones entre casillas
        state = Constants.Init; // Estado inicial
    }

    /// <summary>
    /// Inicializa las casillas del tablero y coloca las fichas en sus posiciones iniciales
    /// </summary>
    void InitTiles()
    {
        // Recorre todas las filas y columnas del tablero
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                // Obtiene cada casilla y la almacena en el array tiles
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();
            }
        }

        // Coloca las fichas en sus posiciones iniciales        
        cops[0].GetComponent<CopMove>().currentTile = Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile = Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile = Constants.InitialRobber;
    }

    /// <summary>
    /// Establece las conexiones entre casillas adyacentes (arriba, abajo, izquierda, derecha)
    /// </summary>
    public void InitAdjacencyLists()
    {
        // Matriz que indica qué casillas están conectadas
        bool[,] matriu = new bool[Constants.NumTiles, Constants.NumTiles];

        // Para cada casilla, establece conexiones con sus vecinos
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            int row = i / Constants.TilesPerRow; // Fila actual
            int col = i % Constants.TilesPerRow; // Columna actual

            // Conexión con casilla de arriba (si no está en la primera fila)
            if (row > 0) matriu[i, i - Constants.TilesPerRow] = true;

            // Conexión con casilla de abajo (si no está en la última fila)
            if (row < Constants.TilesPerRow - 1) matriu[i, i + Constants.TilesPerRow] = true;

            // Conexión con casilla izquierda (si no está en la primera columna)
            if (col > 0) matriu[i, i - 1] = true;

            // Conexión con casilla derecha (si no está en la última columna)
            if (col < Constants.TilesPerRow - 1) matriu[i, i + 1] = true;
        }

        // Rellena la lista de adyacencia de cada casilla
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            tiles[i].adjacency.Clear(); // Limpia lista existente

            // Añade índices de casillas vecinas
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                if (matriu[i, j])
                {
                    tiles[i].adjacency.Add(j);
                }
            }
        }
    }

    /// <summary>
    /// Reinicia el estado de todas las casillas
    /// </summary>
    public void ResetTiles()
    {
        foreach (Tile tile in tiles)
        {
            tile.Reset(); // Llama al método Reset de cada casilla
        }
    }

    /// <summary>
    /// Maneja el clic en un policía
    /// </summary>
    /// <param name="cop_id">Índice del policía clickeado (0 o 1)</param>
    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:
                clickedCop = cop_id; // Almacena qué policía se ha seleccionado
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true; // Marca la casilla actual

                ResetTiles(); // Limpia selecciones anteriores
                FindSelectableTiles(true); // Encuentra movimientos posibles

                state = Constants.CopSelected; // Cambia estado                
                break;
        }
    }

    /// <summary>
    /// Maneja el clic en una casilla del tablero
    /// </summary>
    /// <param name="t">Índice de la casilla clickeada</param>
    public void ClickOnTile(int t)
    {
        clickedTile = t;

        switch (state)
        {
            case Constants.CopSelected:
                // Si la casilla es seleccionable, mueve el policía
                if (tiles[clickedTile].selectable)
                {
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;

                    state = Constants.TileSelected; // Cambia estado
                }
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    /// <summary>
    /// Finaliza el turno actual y pasa al siguiente estado
    /// </summary>
    public void FinishTurn()
    {
        switch (state)
        {
            case Constants.TileSelected:
                // Termina turno del policía, pasa turno al ladrón
                ResetTiles();
                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:
                // Termina turno del ladrón, incrementa ronda
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false); // Pierde si se acaban las rondas
                break;
        }
    }

    /// <summary>
    /// Controla el turno del ladrón (IA)
    /// </summary>
    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false); // Encuentra movimientos posibles

        // Filtra casillas seleccionables
        List<Tile> selectableTiles = tiles.Where(t => t.selectable).ToList();

        if (selectableTiles.Count > 0)
        {
            // IA Inteligente: Calcula distancia a policías para cada casilla posible
            Dictionary<Tile, int> tileScores = new Dictionary<Tile, int>();
            foreach (Tile tile in selectableTiles)
            {
                // Encuentra la distancia mínima a cualquier policía
                int minCopDistance = cops.Min(cop =>
                    BFS_CalculateMinDistance(tile.numTile, cop.GetComponent<CopMove>().currentTile));
                tileScores[tile] = minCopDistance;
            }

            // Elige la casilla más alejada de los policías
            Tile bestTile = tileScores.OrderByDescending(kv => kv.Value).First().Key;
            robber.GetComponent<RobberMove>().MoveToTile(bestTile);
            robber.GetComponent<RobberMove>().currentTile = bestTile.numTile;
        }
    }

    /// <summary>
    /// Calcula la distancia mínima entre dos casillas usando BFS
    /// </summary>
    /// <param name="start">Casilla de inicio</param>
    /// <param name="target">Casilla objetivo</param>
    /// <returns>Distancia mínima entre las casillas</returns>
    private int BFS_CalculateMinDistance(int start, int target)
    {
        if (start == target) return 0; // Misma casilla

        // BFS estándar para encontrar distancia mínima
        bool[] visited = new bool[Constants.NumTiles];
        Queue<(int pos, int dist)> queue = new Queue<(int, int)>();
        queue.Enqueue((start, 0));
        visited[start] = true;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // Revisa todos los vecinos
            foreach (int neighbor in tiles[current.pos].adjacency)
            {
                if (neighbor == target) return current.dist + 1; // Encontrado

                if (!visited[neighbor])
                {
                    visited[neighbor] = true;
                    queue.Enqueue((neighbor, current.dist + 1));
                }
            }
        }

        return int.MaxValue; // No debería ocurrir en un tablero conexo
    }

    /// <summary>
    /// Encuentra las casillas a las que se puede mover una ficha (BFS limitado a 2 movimientos)
    /// </summary>
    /// <param name="cop">True si es turno de policía, False si es turno del ladrón</param>
    public void FindSelectableTiles(bool cop)
    {
        // Determina la casilla actual según qué ficha se está moviendo
        int indexcurrentTile = cop ? cops[clickedCop].GetComponent<CopMove>().currentTile :
                                  robber.GetComponent<RobberMove>().currentTile;

        // Resetea todas las casillas
        foreach (Tile tile in tiles)
        {
            tile.Reset();
            tile.selectable = false;
        }

        // Marca la casilla actual
        tiles[indexcurrentTile].current = true;
        tiles[indexcurrentTile].visited = true;

        // Cola para BFS que almacena (casilla, camino, distancia)
        Queue<(Tile node, List<int> path, int distance)> nodes = new Queue<(Tile, List<int>, int)>();
        nodes.Enqueue((tiles[indexcurrentTile], new List<int> { indexcurrentTile }, 0));

        // Obtiene posiciones de los policías para evitar movimientos inválidos
        HashSet<int> copPositions = new HashSet<int>(
            cops.Select(c => c.GetComponent<CopMove>().currentTile)
        );

        // Algoritmo BFS para encontrar casillas alcanzables
        while (nodes.Count > 0)
        {
            var current = nodes.Dequeue();
            Tile currentNode = current.node;
            List<int> currentPath = current.path;
            int currentDistance = current.distance;

            if (currentDistance >= 2) continue; // Limite de 2 movimientos

            // Revisa todos los vecinos
            foreach (int neighborIndex in currentNode.adjacency)
            {
                Tile neighbor = tiles[neighborIndex];

                // Si no ha sido visitado y no está ocupado por policía
                if (!neighbor.visited && !copPositions.Contains(neighborIndex))
                {
                    neighbor.visited = true;
                    neighbor.parent = currentNode;
                    neighbor.distance = currentDistance + 1;

                    // Almacena el camino completo
                    List<int> newPath = new List<int>(currentPath) { neighborIndex };
                    neighbor.pathToTile = newPath;

                    // Marca como seleccionable (excepto posición inicial)
                    if (newPath.Count > 1 && !copPositions.Contains(neighborIndex))
                    {
                        neighbor.selectable = true;
                    }

                    nodes.Enqueue((neighbor, newPath, currentDistance + 1));
                }
            }
        }
    }

    /// <summary>
    /// Finaliza el juego mostrando el mensaje correspondiente
    /// </summary>
    /// <param name="end">True si gana el jugador, False si pierde</param>
    public void EndGame(bool end)
    {
        if (end)
        {
            finalMessage.text = "Los policias han ganado!"; // Ladrón capturado
            finalMessage.color = new Color(0.1f, 0.8f, 0.1f);
            Debug.Log("Color should be green");
        }
        else
            finalMessage.text = "Pediste. El ladrón se escapó.!"; // Se acabaron las rondas
            finalMessage.color = Color.red; 
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    /// <summary>
    /// Reinicia el juego a su estado inicial
    /// </summary>
    public void PlayAgain()
    {
        // Vuelve a colocar las fichas en sus posiciones iniciales
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);

        ResetTiles(); // Resetea el tablero

        // Resetea la interfaz
        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    /// <summary>
    /// Inicializa un nuevo juego
    /// </summary>
    public void InitGame()
    {
        state = Constants.Init;
    }

    /// <summary>
    /// Incrementa el contador de rondas y actualiza el texto
    /// </summary>
    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }
}