using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;


public class Coord
{
    public int R { get; set; }
    public int C { get; set; }

    public Coord(int r, int c)
    {
        R = r;
        C = c;
    }
    public bool Equals(Coord coordObj)
    {
       if (coordObj == null)
            return false;
        return (coordObj.C == this.C && coordObj.R == this.R);
    }
    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;

        Coord coordObj = obj as Coord;
        return Equals(coordObj);
    }
    public override int GetHashCode()
    {
        return ( this.C + this.R ) / 2;
    }
    public static bool operator ==(Coord c1, Coord c2)
    {
        if (((object)c1) == null)
            return Object.Equals(c1, c2);
        else
            return c1.Equals(c2);
    }

    public static bool operator !=(Coord c1, Coord c2)
    {
        if (((object)c1) == null)
            return !Object.Equals(c1, c2);
        else
            return !c1.Equals(c2);
    }
}
public class SearchNode
    {
    public static SearchNode[,] searchTree;
    public static SearchNode startNode,finalNode;

    public static int C, R, A;
    public const int DOWN = 0;
    public const int UP = 1;
    public const int LEFT = 2;
    public const int RIGHT = 3;
    public static string[] Commands = {"DOWN","UP","LEFT","RIGHT"};
    public static int[] AllDirections = { DOWN, UP, LEFT, RIGHT };
    public static int[] InverseDirection = { UP, DOWN, RIGHT, LEFT };
    public static int[][] ReverseDirections = new int[][]
     { new int[]{ DOWN,LEFT,RIGHT }, new int[]{ UP, LEFT, RIGHT }, new int[]{ DOWN, UP, LEFT }, new int[]{ DOWN, UP, RIGHT } };

    private SearchNode[] nodes = new SearchNode[4];
    public SearchNode Down { get => nodes[DOWN]; set => nodes[DOWN] = value; }
    public SearchNode Up { get => nodes[UP]; set => nodes[UP] = value; }
    public SearchNode Left { get => nodes[LEFT]; set => nodes[LEFT] = value; }
    public SearchNode Right { get => nodes[RIGHT]; set => nodes[RIGHT] = value; }

    public Coord Point { get; }

    // nb of usefull nodes, not null, not deadend
    private int nbNodes;

    public bool Deadend { get => (nbNodes <= 1);  } // meaning no more exit in the leaf => optimization

    // minimal to go to T / value 0 at the point T
    public int StepsToT { get ; set ; }
    // minimal to go back to K / value 0 at the point K
    public int StepsToK { get; set; }
    public bool Final { get => (finalNode != null && this.Point == SearchNode.finalNode.Point); }
    public bool Start { get => (startNode != null && this.Point == SearchNode.startNode.Point); }
    //  leafs with ?
    public bool Unknown { get; set; }
    public SearchNode(Coord point, int stepsToT, int stepsToK, bool unknwown = true)
    {
        Point = point;
        nbNodes = 4;
        StepsToT = stepsToT;
        StepsToK = stepsToT;
        Unknown = unknwown;
    }

    public static void OptimizePathsToStart(SearchNode a, SearchNode b)
    {
        if (a != null && b != null)
        {
            if (a.StepsToT > b.StepsToT)
            {
                a.FoundShortCutsToStart(b.StepsToT);
            }
            else if (a.StepsToT > b.StepsToT)
            {
                b.FoundShortCutsToStart(a.StepsToT);
            }
        }
    }

    private void FoundShortCutsToStart(int stepsToT)
    {
        if (this.StepsToT > stepsToT)
        {
            this.StepsToT = stepsToT;
            foreach (SearchNode n in this.nodes)
            {
                if (n != null)
                    n.FoundShortCutsToStart(stepsToT + 1);
            }
        }        
    }

    private int FoundShortCutsToK(int stepsToK)
    {
        if (this.StepsToK > stepsToK)
        {
            this.StepsToK = stepsToK;
            foreach (SearchNode n in this.nodes)
            {
                if (n != null)
                    n.FoundShortCutsToK(stepsToK + 1);
            }
        }
        return this.StepsToK;
    }
    
    /// <summary>
    /// check if a exit is possible and if not ... optimize the previous path 
    /// </summary>
    public void ProcessDeadend()
    {
            nbNodes = 0;
            foreach (SearchNode n in this.nodes)
            {
                if (n != null && ( !n.Deadend || n.Unknown || n.Final ) )
                {
                    nbNodes++;
                }
            }
    }

    /// <summary>
    /// return the coordonate of the point next to the direction : RIGHT, ...
    /// or null if in the frontier/limits
    /// </summary>
    /// <param name="posR"></param>
    /// <param name="posC"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    private static Coord NearCoord(int posR, int posC,int direction)
    {
        switch(direction)
        {
            case RIGHT:
                if (posC == SearchNode.C - 1) return null;
                return new Coord(posR, posC + 1);
            case LEFT:
                if (posC == 0) return null;
                return new Coord(posR, posC - 1);
            case UP:
                if (posR == 0) return null;
                return new Coord(posR - 1 , posC);
            default:
            case DOWN:
                if (posR == SearchNode.R - 1) return null;
                return new Coord(posR + 1, posC);
        }
    }

    /// <summary>
    /// exploration routine :  strategy is to explore all the displayed zone. in order to build the SearchTree  , with the deadend possible
    /// </summary>
    /// <param name="lab">lab matrix</param>
    /// <param name="KR">position Row</param>
    /// <param name="KC">position Column</param>
    /// <param name="directionsToExplore"></param>
    /// <param name="stepsFromT"></param>
    public void Explore(char[][] lab, int posR, int posC, int[] directionsToExplore)
    {
        Console.Error.WriteLine("Explore " + posR + " " + posC + " fromT:" + this.StepsToT + " fromK:" + this.StepsToK);

        if (Unknown)
        {
            Unknown = false; //stay to known if any other neighbor is '?'  
            foreach (int d in directionsToExplore)
            {

                SearchNode currentNeightborNode = null;
                Coord neightbor = SearchNode.NearCoord(posR, posC, d);
                if (neightbor != null)
                {
                    currentNeightborNode = SearchNode.searchTree[neightbor.R, neightbor.C];
                    if (currentNeightborNode != null)// already displayed , is it ashortcuts
                    {
                        SearchNode.OptimizePathsToStart(currentNeightborNode, this);                        
                    }
                    else 
                    {
                        currentNeightborNode = this.discoverNode(lab[neightbor.R][neightbor.C], neightbor);
                        SearchNode.searchTree[neightbor.R, neightbor.C] = currentNeightborNode;

                        if (currentNeightborNode != null && !currentNeightborNode.Deadend)
                            currentNeightborNode.Explore(lab, neightbor.R, neightbor.C, ReverseDirections[d]);

                    }
                    nodes[d] = currentNeightborNode;

                    if (currentNeightborNode != null && currentNeightborNode.Final && currentNeightborNode.StepsToT <= SearchNode.A) // final point found
                    {
                        // optimum path found => end the exploration routine
                        return;
                    }
                }                
            }
        }
    }


        // Phase1: for exploring purpose
        public int? MoveKtoExplore(int direction)
    {
        return RIGHT;
    }
    // Phase3: K has to go back to T
    public int? MoveKtoT(int direction)
    {
        return RIGHT;
    }
    // Phase2:  the C is known and the back path is optimized ( stepsToT <= A )
    public int? MoveKtoC(int direction)
        {
            if ((direction >= 45 && direction <= 135) && Right != null && (Right.Unknown || !Right.Deadend))
                return RIGHT;
            if ((direction >= 225 && direction <= 315) && Left != null && (Left.Unknown || !Left.Deadend))
                return LEFT;
            if ((direction >= 315 && direction <= 45) && Up != null && (Up.Unknown || !Up.Deadend))
                return UP;
            if ((direction >= 135 && direction <= 225) && Down != null && (Down.Unknown || !Down.Deadend))
                return DOWN;
            if (Right != null && (!Right.Deadend))
                return RIGHT;
            if (Left != null && (!Left.Deadend))
                return LEFT;
            if (Up != null && (!Up.Deadend))
                return UP;
            if (Down != null && (!Down.Deadend))
                return DOWN;
            return null;
        }
    private SearchNode discoverNode(char? v, Coord c)
    {
        switch (v)
        {
            case '.':
                return new SearchNode(c, this.StepsToT+1, this.StepsToK + 1,true );
            case 'C': // final point
                // found the path
                SearchNode.finalNode = new SearchNode(c, this.StepsToT + 1, this.StepsToK + 1, true);
                return SearchNode.finalNode;
            case 'T':
                // back to a new start?
                SearchNode startNode = new SearchNode(c, 0, this.StepsToK + 1,true);
                return startNode;
            case '?':
                this.Unknown = true;
                return null;
            default:
            case '#':
                return null;
        }
    }

        public override string ToString()
        {
            string s = "stepsToT : " + this.StepsToT + " deadend ? " + this.Deadend;

        if (this.Right != null)
            s += "|right" + " deadend ? " + Right.Deadend; ;
            if (Left != null)
                s += "|left" + " deadend ? " + Left.Deadend; ;
            if (Up != null)
                s += "|up" + " deadend ? " + Up.Deadend; ;
            if (Down != null)
                s += "|down" + " deadend ? " + Down.Deadend; ;

            return s;
        }
    }

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    static void Main(string[] args)
    {
        Console.SetIn(new Codingame_Labyrinth_Console());        

        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int R = int.Parse(inputs[0]); // number of rows.
        int C = int.Parse(inputs[1]); // number of columns.
        int A = int.Parse(inputs[2]); // number of rounds between the time the alarm countdown is activated and the time the alarm goes off.

        char[][] lab = new char[R][];
        SearchNode.C = C;
        SearchNode.R = R;
        SearchNode.A = A;
        SearchNode.searchTree = new SearchNode[R, C];
        Console.Error.WriteLine("C: " + C + " R " + R +" A " + A );

        int TR = -1, TC = -1, CR = -1, CC = -1; //row & column of T and C points
        // List of the SearchNode with ?, the nearest from K is the first
        SortedList<int,SearchNode> unkwnownNodes = new SortedList<int,SearchNode>(); 
        Stack<int> oneWayCommands = new Stack<int>(); // history of the Kirk commands
        int direction = -1; // strategies pour une direction optimisée  : entre 0 et 360  : avec 0 = UP, 180 = DOWN, 90 = RIGHT , 270 = LEFT

        bool oneWay = true; // pahse d exploration
        int nbMouvements = 1200;
        bool Cfound = false;


        while (true)
        {
            inputs = Console.ReadLine().Split(' ');
            int KR = int.Parse(inputs[0]); // row where Kirk is located.
            int KC = int.Parse(inputs[1]); // column where Kirk is located.
            if (TR == -1) // init de la premier position
            {
                SearchNode.startNode = new SearchNode(new Coord(KR, KC), 0, 0);
                unkwnownNodes.Add(0, SearchNode.startNode);
                TR = KR; TC = KC;
                SearchNode.searchTree[TR, TC] = SearchNode.startNode;
            }


            for (int i = 0; i < R; i++)
            {
                string ROW = Console.ReadLine(); // C of the characters in '#.TC?' (i.e. one line of the ASCII maze).
                lab[i] = ROW.ToCharArray();
                if (CC == -1 && (CC = ROW.IndexOf('C')) >= 0)
                {
                    CR = i;
                    Cfound = true;
                    Console.Error.WriteLine("C point found: " + CR + ',' + CC);
                }
            }
            Console.Error.WriteLine("Kirk " + KR + ',' + KC);

            if (lab[KR][KC] == 'C') // Le point de destination a été touché /retour à la base
            {
                oneWay = false;
                direction = -1;
            }

            if (oneWay)
            {
                // exploration du lab affiché
                SearchNode currentPosition = unkwnownNodes[0];


                currentPosition.Explore(lab, KR, KC, SearchNode.AllDirections); // exploration recursive : tant que des nouveaux points ont apparus
                Console.Error.WriteLine("dump current Node: " + currentPosition.ToString());
                
                // direction à rechercher si la cible a été affichée
                if (Cfound)
                {
                    direction = 0;
                    int nb = 1;
                    if (KC < CC)
                    {
                        direction = 90; // Right
                        nb = 2;
                    }
                    else if (KC > CC)
                    {
                        direction = 270; // Left
                        nb = 2;
                    }

                    if (KR > CR)
                    {
                        direction = (direction + 360) / nb; // Up
                    }
                    else if (KR < CR)
                    {
                        direction = (direction + 180) / nb; // down
                    }

                    Console.Error.WriteLine("direction (cible trouvée) : " + direction);
                }


                // pas de direction ou blocage par des murs?  algo de recherche => privilegier le centre
                if (direction == -1)
                {
                    direction = 0;
                    int nb = 1;
                    if ((KC - C / 2) < 0)
                    {
                        direction = 90; // Right
                        nb = 2;
                    }
                    else if ((KC - C / 2) > 0)
                    {
                        direction = 270; // Left
                        nb = 2;
                    }

                    if ((KR - R / 2) > 0)
                    {
                        direction = (direction + 360) / nb; // Up
                    }
                    else if ((KR - R / 2) < 0)
                    {
                        direction = (direction + 180) / nb; // down
                    }


                    Console.Error.WriteLine("direction (deplcement vers le centre?) : " + direction);
                }

                int? nextNode = currentPosition.MoveKtoExplore(direction);


                Console.Error.WriteLine("Best Next Node: " + nextNode);
                if (nextNode != null)
                {
                    Console.WriteLine(SearchNode.Commands[(int)nextNode]); 
                    oneWayCommands.Push((int)nextNode);
                }
                else
                {
                    direction = -1;
                }
            } /// fin de l aller 


            // pas de direction ou alors retour à la base/T: faire marche arriere 
            if (direction == -1)
            {

                int last = SearchNode.LEFT;
                if (oneWayCommands.Count != 0)
                    last = oneWayCommands.Pop();
                Console.WriteLine(SearchNode.Commands[ SearchNode.InverseDirection[last] ]);
            }

        }// end ofwhile
        // Kirk est sur le point C
        // retour à la base T

        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");



    }
}

