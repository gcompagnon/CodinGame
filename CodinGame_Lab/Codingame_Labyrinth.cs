using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public struct UpdateStepNode
    {
    public SearchNode node;
    public int steps;
    public UpdateStepNode(SearchNode s, int st)
    {
        node = s;
        steps = st;
    }
}
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

    public override string ToString()
    {
        return "(R:" + this.R + ",C:" + this.C + ")";
    }
}
/// <summary>
/// Implement a Djistra algo / Tree of SearchNode, each has counts to 2 points : Start and Current
/// @author gcompagnon
/// </summary>
public class SearchNode
    {
    public static SearchNode[,] searchTree;
    public static SearchNode startNode, finalNode, currentNode, targetNode;
    public static Queue<int> pathFinalToStart;
    public static Stack<int> pathCurrentToTarget;

    private static int?[,] stepsToCurrent; // reinstance each turn , each time , the position changes
    private static int?[,] stepsToStart; // contains for each node, the steps from it to start

    // List of the SearchNode with leafs with ?, the nearest from K is the first
    public static Queue<SearchNode> unknownNodes = new Queue<SearchNode>();
    public static Queue<SearchNode> newNodes = new Queue<SearchNode>();    

    // flag if path from Start to Final , found with optimized path
    public static bool OptimizedPathFound;

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

    public bool Final { get => (finalNode != null && this.Point == SearchNode.finalNode.Point); }
    public bool Start { get => (startNode != null && this.Point == SearchNode.startNode.Point); }
    public bool Current { get => (currentNode != null && this.Point == SearchNode.currentNode.Point); }
    //  leafs with ?
    public bool Unknown { get; set; }
    
    // minimal to go back to Current / value 0 at the point Current
    public int StepsToCurrent
    {
        get => (SearchNode.stepsToCurrent[this.Point.R, this.Point.C] ?? -1);
        private set => SearchNode.stepsToCurrent[this.Point.R, this.Point.C] = value;
    }
    // minimal to go to start / value 0 at the point Start
    public int StepsToStart
    {
        get => (SearchNode.stepsToStart[this.Point.R, this.Point.C] ?? -1);
        private set => SearchNode.stepsToStart[this.Point.R, this.Point.C] = value;
    }
    private SearchNode(Coord point, int stepsToStart, int stepsToCurrent)
    {
        Point = point;
        nbNodes = 4;
        Unknown = true;
        StepsToStart = stepsToStart;
        StepsToCurrent = stepsToCurrent;        
    }

    #region static utils
    /// <summary>
    /// Build the first Node (start) and init the Tree
    /// </summary>
    /// <param name="r">dimension in rows</param>
    /// <param name="c">dimension in columns</param>
    /// <param name="a">nb de max steps for the back return path</param>
    /// <param name="startR">start position</param>
    /// <param name="startC">start position</param>
    public static void InitSearchTree(int r, int c, int a, int startR, int startC)
    {
        if (startR >= 0 && startR <= r && startC >= 0 && startC <= c)
        {
            SearchNode.C = c;
            SearchNode.R = r;
            SearchNode.A = a;
            SearchNode.stepsToCurrent = new int?[SearchNode.R, SearchNode.C];
            SearchNode.stepsToStart = new int?[SearchNode.R, SearchNode.C];
            SearchNode.searchTree = new SearchNode[r, c];
            SearchNode.startNode = new SearchNode(new Coord(startR, startC), 0, 0);
            SearchNode.searchTree[startR, startC] = SearchNode.startNode;
            SearchNode.OptimizedPathFound = false;
        }
    }
    public static SearchNode AddNewNodeSearchTree(Coord c, int stepsToStart, int stepsToCurrent)
    {
        SearchNode node = new SearchNode(c, stepsToStart, stepsToCurrent);
        SearchNode.searchTree[c.R, c.C] = node;
        return node;
    }
    public static void CalculatePathsToCurrent()
    {
        SearchNode.stepsToCurrent = new int?[SearchNode.R, SearchNode.C];
        SearchNode.currentNode.CalculatePathsToCurrent(0); 
    }
    public void CalculatePathsToCurrent(int step)
    {
        Queue<UpdateStepNode> toRecalculate = new Queue<UpdateStepNode>();
        toRecalculate.Enqueue(new UpdateStepNode(this, step));
        while (toRecalculate.Count > 0)
        {
            UpdateStepNode c = toRecalculate.Dequeue();
            if (!c.node.Deadend)
                c.node.SetPathToCurrent(c.steps, toRecalculate);
        }
    }

    public static void CalculatePathsToStart()
    {
        SearchNode.startNode.CalculatePathsToStart(0);
    }

    public void CalculatePathsToStart(int step)
    {
        Queue<UpdateStepNode> toRecalculate = new Queue<UpdateStepNode>();
        toRecalculate.Enqueue(new UpdateStepNode(this, step));
        while (toRecalculate.Count > 0)
        {
            UpdateStepNode c = toRecalculate.Dequeue();
            if (!c.node.Deadend)
                c.node.SetPathToStart(c.steps, toRecalculate);
        }
    }

    /// <summary>
    /// return the coordonate of the point next to the direction : RIGHT, ...
    /// or null if in the frontier/limits
    /// </summary>
    /// <param name="posC"></param>
    /// <param name="direction">RIGHT LEFT UP or DOWN</param>
    /// <returns></returns>
    public static Coord NearCoord(Coord posC, int direction)
    {
        switch (direction)
        {
            case RIGHT:
                if (posC.C == SearchNode.C - 1) return null;
                return new Coord(posC.R, posC.C + 1);
            case LEFT:
                if (posC.C == 0) return null;
                return new Coord(posC.R, posC.C - 1);
            case UP:
                if (posC.R == 0) return null;
                return new Coord(posC.R - 1, posC.C);
            default:
            case DOWN:
                if (posC.R == SearchNode.R - 1) return null;
                return new Coord(posC.R + 1, posC.C);
        }
    }
    #endregion

    #region public
    /// <summary>
    /// exploration routine :  strategy is to explore all the displayed zone. in order to build the SearchTree  , with the deadend possible
    /// </summary>
    /// <param name="lab">lab matrix</param>
    public void Explore(char[][] lab)
    {        
        if (Unknown)
        {
            Unknown = false; //stay to known if any other neighbor is '?'  
            foreach (int d in SearchNode.AllDirections)
            {
                SearchNode currentNeightborNode = null;
                Coord neightbor = SearchNode.NearCoord(this.Point, d);
                if (neightbor != null) // if not on a frontier
                {
                    currentNeightborNode = SearchNode.searchTree[neightbor.R, neightbor.C];
                    nodes[d] = currentNeightborNode;

                    if (currentNeightborNode == null)
                    {
                        currentNeightborNode = this.DiscoverNode(lab[neightbor.R][neightbor.C], neightbor);
                        nodes[d] = currentNeightborNode;
                        if (currentNeightborNode != null && !SearchNode.OptimizedPathFound)
                        {// add a node to explore                            
                            SearchNode.unknownNodes.Enqueue(currentNeightborNode);                            
                        }
                    }
                    else
                    {
                        // the neightbor has been explored / update Paths if shortcut
                        if (this.StepsToStart> (currentNeightborNode.StepsToStart + 1))
                        {
                            // recalculate StepsStart
                            this.CalculatePathsToStart(currentNeightborNode.StepsToStart + 1);
                        }
                        if (this.StepsToCurrent > (currentNeightborNode.StepsToCurrent + 1))
                        {
                            // recalculate StepsStart
                            this.CalculatePathsToCurrent(currentNeightborNode.StepsToCurrent + 1);
                        }

                    }


                    if (currentNeightborNode!=null && currentNeightborNode.Final && currentNeightborNode.StepsToStart <= SearchNode.A) // final point found
                    {
                        SearchNode.OptimizedPathFound = true;
                        // optimum path found => end the exploration routine
                    }
                  }
            }
            this.ProcessDeadend();

            if(Unknown)
                SearchNode.newNodes.Enqueue(this);
        }
    }

    // Phase1: for exploring purpose
    public static int MoveToTarget(SearchNode target)
    {        
        // change the target if only previous is not reached
        if (target != null && ( SearchNode.targetNode == null || !SearchNode.targetNode.Unknown ))
        {            
            SearchNode.ChangeTarget(target);
        }
        return MoveToTarget();
    }
    public static int MoveToTarget()
    {
        if (SearchNode.pathCurrentToTarget == null || SearchNode.pathCurrentToTarget.Count == 0)
            throw new Exception("No SOLUTION to exit the labyrinth");
        return SearchNode.InverseDirection[SearchNode.pathCurrentToTarget.Pop()];
    }


    public static void ReturnToStart()
    {
        SearchNode.ChangeOrigin(SearchNode.finalNode);
    }

    // Phase3: Current has to go back to Start
    public static int MoveToStart()
    {
        if (SearchNode.pathFinalToStart == null || SearchNode.pathFinalToStart.Count == 0)
            throw new Exception("No SOLUTION to exit the labyrinth");
        return SearchNode.pathFinalToStart.Dequeue();
    }

    // Phase2:  the Final node is known and the back retrn is optimized ( stepsToT <= A )
    public static int MoveToFinal()
    {
        if (SearchNode.targetNode == null || SearchNode.targetNode != SearchNode.finalNode)
        {
            SearchNode.ChangeTarget(SearchNode.finalNode);
        }

        if (SearchNode.pathCurrentToTarget == null || SearchNode.pathCurrentToTarget.Count == 0)
            throw new Exception("No SOLUTION to exit the labyrinth");
        return SearchNode.InverseDirection[SearchNode.pathCurrentToTarget.Pop()];

    }
    #endregion
    #region private
    /// <summary>
    /// rebuild a path FROM Current TO Target
    /// </summary>
    /// <param name="target"></param>
    private static void ChangeTarget(SearchNode target)
    {
        SearchNode.targetNode = target;
        SearchNode.pathCurrentToTarget = new Stack<int>();        
        SearchNode onPathNode = target;
        int steps = target.StepsToCurrent;
        int direction = -1;
        SearchNode n=null;
        int[] directions = SearchNode.AllDirections;
        while (steps > 0)
        {            
            foreach (int d in directions)
            {               
                n = onPathNode.nodes[d];
                if (n != null)
                {
                    if(n.StepsToCurrent == (steps - 1))
                    {
                        direction = d;                        
                        SearchNode.pathCurrentToTarget.Push(d);
                        onPathNode = n;
                    }
                }
            }
            directions = ReverseDirections[direction];                       
            steps--;
        }
    }
    /// <summary>
    /// rebuild a path FROM Origin TO Start
    /// </summary>
    /// <param name="origin"></param>
    private static void ChangeOrigin(SearchNode origin)
    {               
        SearchNode.pathFinalToStart = new Queue<int>();
        SearchNode onPathNode = origin;
        int steps = origin.StepsToStart;
        int direction = -1;
        SearchNode n = null;
        int[] directions = SearchNode.AllDirections;
        while (steps > 0)
        {
            foreach (int d in directions)
            {
                n = onPathNode.nodes[d];
                if (n != null)
                {
                    if (n.StepsToStart == (steps - 1))
                    {
                        direction = d;
                        SearchNode.pathFinalToStart.Enqueue(d);
                        onPathNode = n;
                    }
                }
            }
            directions = ReverseDirections[direction];
            steps--;
        }
    }

    /// <summary>
    /// check if a exit is possible and if not ... optimize the previous path 
    /// </summary>
    private void ProcessDeadend()
    {
        if (this.Final || this.Start || this.Unknown)
            this.nbNodes = 4;
        else
        {
            this.nbNodes = 0;

            foreach (SearchNode n in this.nodes)
            {
                if ((n != null) && (!n.Deadend))
                {
                    this.nbNodes++;
                }
            }
            if (this.Deadend)
            {
                foreach (SearchNode n in this.nodes)
                {
                    if ((n != null) && (!n.Deadend))
                    {
                        n.ProcessDeadend(); ;
                    }

                }
            }
        }
    }
    private void SetPathToCurrent(int steps,Queue<UpdateStepNode> toRecalculate)
    {
        int s = this.StepsToCurrent;
        if (s == -1 || s > steps)
        {
            this.StepsToCurrent = steps;            
            if (this.Final)
                return;

            foreach (SearchNode n in this.nodes)
            {
                if (n != null)
                {
                    toRecalculate.Enqueue(new UpdateStepNode(n, steps + 1));
                }
            }
        }
    }

    private void SetPathToStart(int steps, Queue<UpdateStepNode> toRecalculate)
    {
        if (this.StepsToStart > steps)
        {
            this.StepsToStart = steps;
            if (this.Final)
                return;

            foreach (SearchNode n in this.nodes)
            {
                if (n != null)
                {
                    toRecalculate.Enqueue(new UpdateStepNode(n,steps+1));
                }
            }
        }
    }

    private void FoundShortCutsToStart(int steps)
    {
        this.StepsToStart = steps;
        if (this.Final)
            return;
        foreach (SearchNode n in this.nodes)
        {
            if ((n != null) && (n.StepsToStart > (steps + 1)))
                n.FoundShortCutsToStart(steps + 1);
        }
    }

    private SearchNode DiscoverNode(char? v, Coord c)
    {
        switch (v)
        {
            case '.':
                return SearchNode.AddNewNodeSearchTree(c, this.StepsToStart+1, this.StepsToCurrent +1);
            case 'C': // final point
                // found the path
                SearchNode.finalNode = SearchNode.AddNewNodeSearchTree(c, this.StepsToStart + 1, this.StepsToCurrent + 1);
                return SearchNode.finalNode;
            case 'T':
                // back to a new start?
                SearchNode.startNode = SearchNode.AddNewNodeSearchTree(c, 0, this.StepsToCurrent + 1);
                return SearchNode.startNode;
            case '?':
                this.Unknown = true;                
                return null;
            default:
            case '#':
                return null;
        }
    }
#endregion

    #region override utils
    public bool Equals(SearchNode nodeObj)
    {
        if (nodeObj == null)
            return false;
        return (nodeObj.Point == this.Point);
    }
    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;

        SearchNode coordObj = obj as SearchNode;
        return Equals(coordObj);
    }
    public override int GetHashCode()
    {
        return this.Point.GetHashCode();
    }
    public static bool operator ==(SearchNode n1, SearchNode n2)
    {
        if (((object)n1) == null)
            return Object.Equals(n1, n2);
        else
            return n1.Equals(n2);
    }

    public static bool operator !=(SearchNode n1, SearchNode n2)
    {
        if (((object)n1) == null)
            return !Object.Equals(n1, n2);
        else
            return !n1.Equals(n2);
    }
    public override string ToString()
        {
            return "SearchNode"+this.Point.ToString()+" stepsToStart: " + this.StepsToStart+
            " stepsToCurrent: " + this.StepsToCurrent + "dir:"+(this.Right!=null?"R":"")+(this.Left != null ? "L" : "")+ (this.Up != null ? "U" : "")+(this.Down != null ? "D" : "")+
            (this.Unknown ? "|Unknown" : "") + (this.Deadend?"|Deadend":"") + (this.Final ? "|Final" : "");
        }
    #endregion
}

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    static void Main(string[] args)
    {
        //Console.SetIn(new Codingame_Labyrinth_Console());

        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int R = int.Parse(inputs[0]); // number of rows.
        int C = int.Parse(inputs[1]); // number of columns.
        int A = int.Parse(inputs[2]); // number of rounds between the time the alarm countdown is activated and the time the alarm goes off.

        char[][] lab = new char[R][];
//        Console.Error.WriteLine(" R:" + R + "C: " + C + " A " + A);

        Stack<int> oneWayCommands = new Stack<int>(); // history of the Kirk commands
        int direction = -1; // strategies pour une direction optimisée  : entre 0 et 360  : avec 0 = UP, 180 = DOWN, 90 = RIGHT , 270 = LEFT

        // 3 phases: 
        // Explore   ----------->   Touch Final ------------> Return to Start -------------> END
        //    if optimizedPathfound         currentNode==FinalNode        currentNode==StartNode
        bool explorePhase = true; // explore phase, and touch_FInal phase if true, return phases if false
        int nbMouvements = 1200;


        while (true)
        {
            inputs = Console.ReadLine().Split(' ');
            int KR = int.Parse(inputs[0]); // row where Kirk is located.
            int KC = int.Parse(inputs[1]); // column where Kirk is located.
            if (SearchNode.startNode == null) // init de la premier position
            {
                SearchNode.InitSearchTree(R, C, A, KR, KC);
                SearchNode.unknownNodes.Enqueue(SearchNode.startNode);
            }

            for (int i = 0; i < R; i++)
            {
                string ROW = Console.ReadLine(); // C of the characters in '#.TC?' (i.e. one line of the ASCII maze).

                lab[i] = ROW.ToCharArray();
                Console.Error.WriteLine(ROW);
            }
            
            SearchNode.currentNode = SearchNode.searchTree[KR, KC];
          //Console.Error.WriteLine("Current " + SearchNode.currentNode);
          //Console.Error.WriteLine("Final " + SearchNode.finalNode);


            if (SearchNode.OptimizedPathFound)
            {
                if (SearchNode.currentNode == SearchNode.finalNode)
                {
                    //Console.Error.WriteLine("Phase: Return to start");

                    explorePhase = false;
                    SearchNode.ReturnToStart();
                }
                else if (SearchNode.currentNode == SearchNode.startNode)
                {
                    break; // END
                }
            }

            if (explorePhase)
            {
                // change Target with the nearest Unknow Node from Current
                SearchNode.CalculatePathsToCurrent();
                //Console.Error.WriteLine("dump current Node: " + SearchNode.currentNode.ToString());

                // explore all the previously unknown nodes
                SearchNode n = null;
                while (SearchNode.unknownNodes.Count > 0)
                {
                    n = SearchNode.unknownNodes.Dequeue();
                    if( !n.Deadend )
                        n.Explore(lab);
                }

                if (SearchNode.finalNode != null && SearchNode.finalNode.StepsToStart <= SearchNode.A) // final point found
                {
                    SearchNode.OptimizedPathFound = true;
                    // optimum path found => end the exploration routine
                    SearchNode.unknownNodes.Clear();
                }

                //Console.Error.WriteLine("dump last expl Node: " + n);

            }

            // continue to explore: choose a Unknow node for going to
            if (explorePhase && !SearchNode.OptimizedPathFound)
            {
                SearchNode target = null;

                Queue<SearchNode> temp = SearchNode.unknownNodes;
                SearchNode.unknownNodes = SearchNode.newNodes;
                SearchNode.newNodes = temp;

                if (SearchNode.targetNode == null || !SearchNode.targetNode.Unknown)
                {
                    int nearestFromCurrent = Int32.MaxValue;
                    foreach( SearchNode n in SearchNode.unknownNodes)
                    {
                        if (nearestFromCurrent > n.StepsToCurrent && !n.Deadend)
                        {
                            nearestFromCurrent = n.StepsToCurrent;
                            target = n;
                        }
                    }
                    //Console.Error.WriteLine("direction (explo) P vers: " + target);
                }
                // explore is to go tho the nearest Unknown node
                 direction = SearchNode.MoveToTarget(target);
                 //Console.Error.WriteLine("direction (explo) : " + direction);
            }

            // Touch Final
            if (explorePhase && SearchNode.OptimizedPathFound)
            {
                //Console.Error.WriteLine("direction (TouchFinal) : ");
                direction = SearchNode.MoveToFinal();
            }

            // Return to start 
            if (!explorePhase && SearchNode.OptimizedPathFound)
            {
                //Console.Error.WriteLine("direction (MoveToStart) :");
                direction = SearchNode.MoveToStart();
            }

            Console.WriteLine(SearchNode.Commands[direction]);

        }// end ofwhile
        // Kirk est sur le point C
        // retour à la base T

        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");



    }
}

