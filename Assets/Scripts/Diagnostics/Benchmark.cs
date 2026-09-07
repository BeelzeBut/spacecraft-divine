// ---------------------------------------------------------------------------
//  Benchmark.cs — instrumentare pentru masuratorile din capitolul 8
//
//  Colecteaza date despre navigatie si despre generarea procedurala si le
//  afiseaza in consola. Componenta de raportare se creeaza automat la rulare,
//  deci nu este necesara nicio modificare a scenelor.
//
//  Comenzi (tastatura, in editor):
//     F9   — afiseaza raportul curent
//     F10  — reseteaza contoarele
//
//  Acest fisier si apelurile marcate cu "// [benchmark]" in Pathfinding1.cs,
//  PathRequestManager.cs si LevelGenerator.cs sunt exclusiv pentru masuratori
//  si pot fi eliminate fara a afecta functionarea jocului.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class Benchmark
{
    // ---- navigatie ----------------------------------------------------
    public static long PathNodesExpanded;      // noduri adaugate in multimea inchisa
    public static int  PathSearches;           // cautari incheiate
    public static int  PathSuccesses;          // cautari care au produs un traseu
    public static int  PathCancelHitOther;     // anulari care au lovit o cerere ulterioara
    public static int  PathCancelHarmless;     // anulari fara efect (cererea se incheiase)
    public static long PathWaypoints;          // total puncte de traseu produse
    static double pathMillisTotal;
    static double pathMillisMax;

    // ---- generare procedurala -----------------------------------------
    public static int GenAttempts;             // incercari de generare (inclusiv respinse)
    public static int GenRejections;           // harti eliminate de criteriul de validare
    public static int GenLevels;               // niveluri generate cu succes
    public static long GenRooms;               // total camere produse
    static double genMillisTotal;
    static readonly Stopwatch genClock = new Stopwatch();

    // ---- navigatie: apelat din Pathfinding1 ----------------------------
    public static void RecordSearch(double millis, bool success, int waypoints)
    {
        PathSearches++;
        pathMillisTotal += millis;
        if (millis > pathMillisMax) pathMillisMax = millis;
        if (success) { PathSuccesses++; PathWaypoints += waypoints; }
    }

    // ---- generare: apelate din LevelGenerator --------------------------
    public static void GenAttempt()
    {
        GenAttempts++;
        if (!genClock.IsRunning) genClock.Restart();
    }

    public static void GenRejected()
    {
        GenRejections++;
    }

    public static void GenSucceeded(int rooms)
    {
        if (genClock.IsRunning)
        {
            genClock.Stop();
            genMillisTotal += genClock.Elapsed.TotalMilliseconds;
        }
        GenLevels++;
        GenRooms += rooms;
    }

    public static void Reset()
    {
        PathNodesExpanded = PathSearches = PathSuccesses = 0;
        PathCancelHitOther = PathCancelHarmless = 0;
        PathWaypoints = 0;
        pathMillisTotal = pathMillisMax = 0;
        GenAttempts = GenRejections = GenLevels = 0;
        GenRooms = 0;
        genMillisTotal = 0;
        genClock.Reset();
        Debug.Log("[Benchmark] contoare resetate");
    }

    public static string Report()
    {
        var sb = new StringBuilder();
        sb.AppendLine("================ MASURATORI (cap. 8) ================");

        var grid = Grid1.instance;
        if (grid != null)
        {
            int nx = Mathf.RoundToInt(grid.gridWorldSize.x / (grid.nodeRadius * 2f));
            int ny = Mathf.RoundToInt(grid.gridWorldSize.y / (grid.nodeRadius * 2f));
            sb.AppendLine($"GRILA        {nx} x {ny} = {nx * ny} noduri | raza nod {grid.nodeRadius} u");
        }

        sb.AppendLine("--- NAVIGATIE ---------------------------------------");
        if (PathSearches > 0)
        {
            sb.AppendLine($"  cautari incheiate         {PathSearches}");
            sb.AppendLine($"  reusite                   {PathSuccesses}  ({100.0 * PathSuccesses / PathSearches:F1} %)");
            int __cancels = PathCancelHitOther + PathCancelHarmless;
            sb.AppendLine($"  anulari declansate        {__cancels}  ({(PathSearches > 0 ? 100.0 * __cancels / PathSearches : 0):F1} % din cautari)");
            sb.AppendLine($"    din care au abandonat");
            sb.AppendLine($"    o cerere in curs         {PathCancelHitOther}   <-- efect real al defectului");
            sb.AppendLine($"    fara efect               {PathCancelHarmless}");
            sb.AppendLine($"  durata medie / cautare    {pathMillisTotal / PathSearches:F3} ms");
            sb.AppendLine($"  durata maxima             {pathMillisMax:F3} ms");
            sb.AppendLine($"  noduri explorate / cautare {(double)PathNodesExpanded / PathSearches:F1}");
            sb.AppendLine($"  noduri explorate (total)  {PathNodesExpanded}");
            if (PathSuccesses > 0)
                sb.AppendLine($"  lungime medie traseu      {(double)PathWaypoints / PathSuccesses:F1} puncte");
            if (pathMillisTotal > 0)
                sb.AppendLine($"  debit teoretic            {1000.0 * PathSearches / pathMillisTotal:F0} cautari/s");
        }
        else sb.AppendLine("  (nicio cautare inregistrata)");

        sb.AppendLine("--- GENERARE PROCEDURALA ----------------------------");
        if (GenAttempts > 0)
        {
            sb.AppendLine($"  incercari de generare     {GenAttempts}");
            sb.AppendLine($"  respinse la validare      {GenRejections}");
            sb.AppendLine($"  RATA DE RESPINGERE        {100.0 * GenRejections / GenAttempts:F1} %");
            sb.AppendLine($"  niveluri produse          {GenLevels}");
            if (GenLevels > 0)
            {
                sb.AppendLine($"  camere / nivel            {(double)GenRooms / GenLevels:F1}");
                sb.AppendLine($"  incercari / nivel         {(double)GenAttempts / GenLevels:F2}");
                sb.AppendLine($"  durata medie / nivel      {genMillisTotal / GenLevels:F1} ms  (inclusiv reincercarile)");
            }
        }
        else sb.AppendLine("  (nicio generare inregistrata)");

        sb.AppendLine("=====================================================");
        return sb.ToString();
    }

    // ---- raportorul, creat automat la rulare ---------------------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (Object.FindObjectOfType<BenchmarkReporter>() != null) return;
        var go = new GameObject("[Benchmark]");
        go.AddComponent<BenchmarkReporter>();
        Object.DontDestroyOnLoad(go);
        Debug.Log("[Benchmark] activ — F9 = raport, F10 = reset");
    }
}

public class BenchmarkReporter : MonoBehaviour
{
    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.f9Key.wasPressedThisFrame)  Debug.Log("\n" + Benchmark.Report());
        if (kb.f10Key.wasPressedThisFrame) Benchmark.Reset();
    }

    void OnApplicationQuit()
    {
        Debug.Log("\n[Benchmark] raport final:\n" + Benchmark.Report());
    }
}
