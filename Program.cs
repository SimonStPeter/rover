using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;

using static rover.RoverUtils;
using System.Diagnostics;



#nullable enable

// Standard exceptions <https://docs.microsoft.com/en-us/dotnet/api/system.exception?view=netframework-4.7.2&f1url=%3FappId%3DDev16IDEF1%26l%3DEN-US%26k%3Dk(System.Exception);k(TargetFrameworkMoniker-.NETFramework,Version%253Dv4.7.2);k(DevLang-csharp)%26rd%3Dtrue#Standard>

// class EX_ROVER_BASE : Exception { };

namespace rover {


    class Program {

        static void Main(string[] args) {

            suppressOutput = true;
            UnitTests.run();
            suppressOutput = false;

            showStartupMsg();

            must<ArgumentException>(args.Count() == 1,
                "Rover must be passed one argument; "
                + "the full path of the Movements file");

            var fileloc = args[0];
            // mfc = movement file contents
            var mfcLines = readLines(fileloc);

            var map = new Map();
            foreach (var line in mfcLines) {
                // in movement file, blank lines allowed,
                // and # as first char makes a comment
                if ((line.Trim() != "") && !line.StartsWith("#")) {
                    var rover = Rover.roverFromStr(line);
                    rover.go(500, map);
                }
            }

            outputErrMsg("Finished! Press any key");
            Console.ReadKey();

        }
    }




    public abstract class Direction {
        private char dir;  // direction
        private char arw;  // arrow; visual indicator  <,>,^,V
        private int xInc, yInc; // for a direction, add these to move forward
        private Func<Direction> leftTurn, rightTurn;

        public (int, int) getIncrements() => (xInc, yInc);

        public Direction(
                    char dirIn, char arwIn,
                    int xIncIn, int yIncIn,
                    Func<Direction> leftTurnIn, Func<Direction> rightTurnIn) {
            // Would normally assert is N,S,E,W etc. but skip here
            (dir, arw) = (dirIn, arwIn);
            (xInc, yInc) = (xIncIn, yIncIn);
            (leftTurn, rightTurn) = (leftTurnIn, rightTurnIn);
        }


        public virtual Direction update(char c) {
            must<InvariantFailure>(
                c == 'L' || c == 'R',
                "Invalid turn character found: " + c);

            Direction res;
            if (c == 'L') {
                res = leftTurn();
            } else {
                res = rightTurn();
            }

            return res;
        }


        // need to create initial direction from a char
        public static Direction directionFromChar(char c) {
            // direction objects produce new direction objects but
            // the initial direction is a letter, so create here

            Direction res = c switch {
                'N' => new North(),
                'S' => new South(),
                'E' => new East(),
                'W' => new West(),
                // must() here won't work, 
                _ => throw new NotImplementedException("Unrecognised letter: " + c)
            };

            return res;
        }


        public char arrow => arw;

    }  // Direction




    public class North : Direction {
        public North() : base(
            'N', '^',
            0, 1, // increments to move north
            // if facing North, a left turn L faces you West, a 
            // right turn R faces you east
            () => new West(), () => new East()
            ) { }
    }


    public class South : Direction {
        public South() : base(
            'S', 'V',
            0, -1,
            () => new East(), () => new West()
            ) { }
    }


    public class East : Direction {
        public East() : base(
            'E', '>',
            1, 0,
            () => new North(), () => new South()
            ) { }
    }


    public class West : Direction {
        public West() : base(
            'W', '<',
            -1, 0,
            () => new South(), () => new North()
            ) { }
    }




    public class Position {
        private int x, y; // position in map

        public (int, int) position => (x, y);

        public Position(int xIn, int yIn) {
            (x, y) = (xIn, yIn);

            must<ArgumentOutOfRangeException>(
                xInRange(x) && yInRange(y),
                "X or Y co-ordinates invalid, are: " + x + ", " + y);
        }


        public Position update(Direction d) {
            var (xi, yi) = d.getIncrements();
            var res = new Position(x + xi, y + yi);

            return res;
        }

    }  // Position




    public class Rover {
        private string commands;
        private Direction dir;
        private Position pos;

        public Rover(Direction dirIn, Position posIn,
                                    string commandsIn) =>
            (dir, pos, commands) = (dirIn, posIn, commandsIn);


        public void go(int animationSpeedMS, Map map) {
            showDebug("init");
            map.update(pos, dir.arrow);
            map.print();

            foreach (var m in commands) {
                /* I could do                
                    pos = pos.update(m);
                    dir = dir.update(m, pos);
                and have pos ignore an L/R and dir ignore M
                but that's relying on implicit assumptions which 
                are fragile, I prefer to make things explicit.
                Not so OO here but ok.
                */
                map.update(pos, 'X'); // mark old position

                if (m == 'M') {
                    pos = pos.update(dir);
                } else {
                    dir = dir.update(m);
                }

                map.update(pos, dir.arrow);
                map.print();
                showDebug("updated");
                Thread.Sleep(animationSpeedMS);
            }

        }


        public static Rover roverFromStr(string s) {
            int x; int y; char initDir; string commands;
            parseLine(s, out x, out y, out initDir, out commands);

            var pos = new Position(x, y);
            var dir = Direction.directionFromChar(initDir);
            var res = new Rover(dir, pos, commands);

            return res;
        }


        public void mustBeAt(int x, int y) {
            var (currX, currY) = pos.position;
            must<UnexpectedRoverLocation>(
                (x == currX) && (y == currY),
                $"Rover is where expected; should be at {x}, {y}"
                + $"but thinks it is at {currX}, {currY}");
        }


        public void showDebug(string msg) {
            var (x, y) = pos.position;
            outputErrMsg($"{msg}:  ({x}, {y}) {dir.arrow}");
        }

    }  // Rover




    public class Map {
        private char[,] m;

        // Nseted for loops copied and pasted,
        // not pretty but ok for now. I'd normally
        // abstract this somehow.

        public Map() {
            m = new char[maxX, maxY];

            for (var x = 0; x < maxX; x++) {
                for (var y = 0; y < maxY; y++) {
                    m[x, y] = '.';
                }
            }
        }


        public void print() {
            for (var x = 0; x < maxX; x++) {
                for (var y = 0; y < maxY; y++) {
                    outputForGrid(x, y, m[x, y]);
                }
            }
        }


        public void update(Position p, char c) {
            var (x, y) = p.position;
            m[x, y] = c;
        }


        public void mustBeFullyTraversed() {
            var covered = true;
            for (var x = 0; x < maxX; x++) {
                for (var y = 0; y < maxY; y++) {
                    covered &= (m[x, y] != '.');
                }
            }

            must<MapNotFullyTraversed>(
                covered,
                "The map was not fully covered by rover(s)");
        }

    }  // Map





    public class InvariantFailure : ApplicationException { 
        // Custom exception.
        // Basically something's gone catastrophically wrong
        // and I can't recover.  Fatal Assert in other words.
    }


    public class UnexpectedRoverLocation : ApplicationException {
        // In testing, rover didn't end up where expected
    }


    public class MapNotFullyTraversed : ApplicationException {
        // Some map point were not reached by rovers
    }



    public class RoverUtils {
        // Utility functions and conveniences

        // Various failures cause err msgs writes when they
        // fail, which they deliberately do in unit tests,
        // so within tests we want to disable output
        public static bool suppressOutput;

        // I had previously done these as statics, but static
        // fields are initialised in a specific order which is
        // easy to get wrong with no warning, in which case you
        // just get an empty string instead of what you expect,
        // or a zero instead of the value et cetera. Doing this
        // makes things much less fragile -- I lost a lot of
        // time with static fields.

        public static string nl =>
            // I have had occassional baffling problems with CR+LF
            // and found this just works. I don't know why.
            @"
";

        // valid grid limits. Low are always going to be zero
        // so a bit pointless.
        public static int minX => 0;
        public static int maxX => 6;
        public static int minY => 0;
        public static int maxY => 6;

        /* getting too clever, just write *InRange funcs directly
        private static Func<int, bool> 
                    makeRangeChecker(int lo, int hi) =>
            (int i) => (i >= lo) && (i <= hi);
        */

        public static bool xInRange(int x) =>
            (x >= minX) && (x < maxX);

        public static bool yInRange(int y) =>
            (y >= minY) && (y < maxY);


        private static void output(int x, int y, string msg) {
            if (!suppressOutput) {
                Console.SetCursorPosition(x, y);
                System.Console.WriteLine(msg);
            }
        }


        public static void outputErrMsg(string msg) {
            output(0, 5, new string(' ', 100));  // clear old msg
            output(0, 5, msg);
        }


        // Moves the bottom of the map down
        // this many places distance
        public static int gridOffset => 20;

        public static void outputForGrid(int x, int y, char c) =>
            output(x, gridOffset - y, c.ToString());


        public static void showStartupMsg() =>
            output(0, 0, "When run from the VS environment in debug mode, "
                + "the movement file will be taken from the debug "
                + "settings, which probably won't work for you. "
                + "These debug settings are found by right-clicking "
                + "on the 'rover' project, picking 'properties', "
                + "picking 'debug' at the left, look for "
                + "'Command Line Arguments' text box. "
                + "Alter the file path to point to the project's "
                + "\\movements_file\\movements.txt directory. "
                + "Alternatively just run the .exe directly "
                + "and pass the full file path on the CLI.");


        public static void must<EX>(bool b, string msg)
                                where EX: Exception, new() {
            if (!b) {
                outputErrMsg(msg);
                throw new EX();
            }
        }


        static public void quit(string msg) {
            outputErrMsg(msg);
            System.Environment.Exit(1);
        }


        public static string[] readLines(string filespec) {
            must< FileNotFoundException>(
                System.IO.File.Exists(filespec),
                " file does not exist: " + filespec);

            var input = System.IO.File.ReadAllLines(filespec);

            return input;
        }


        public static void parseLine(string lineOrig,
            out int x, out int y, out char initDir, out string commands) {
            // assume starting locations are separated by one space eg.
            //    4 4 E
            // and are one digit, and no other spaces are present, and
            // the movements part has at least one LRM character. 
            // Must be ASCII (not checked)

            /*
            var rexPattern = @"^(?<x>\d) (?<y>\d) (?<orientation>[NSEWnsew])>|(?<movements>[LRMlrm]+)$";
            var rex = new Regex(rexPattern, RegexOptions.IgnoreCase);
            var matches = rex.Match(line);

            Every time I've tried to do anything straightforward with regexps 
            the byzantine library trips me up and costs loads of time. I 
            really should have learnt by now.
            Give up and just do it by hand. Gives me a chance for a 
            more forgiving validation.
            */

            // Some of these exceptions 

            var ftpErrMsg =
                "Failed to parse line of input file. "
                + nl + "Line was: " + lineOrig
                + nl;

            var lineU = lineOrig.ToUpper();
            // sp = starting position,  cmds = commands
            var spAndCmds = lineU.Split('|');

            must<ArgumentException>(
                spAndCmds.Length == 2,
                ftpErrMsg + "Could not find vertical bar '|'");

            var (sp, cmds) = (spAndCmds[0], spAndCmds[1]);

            // check movements

            must<ArgumentException>(
                cmds.Length > 0,
                ftpErrMsg + "Movement part was empty");

            var badCharsBag = cmds.Where(ch => 
                            (ch != 'L') && (ch != 'R') && (ch != 'M'));
            var badChars = new string(badCharsBag.ToArray());
            must<ArgumentException>(
                badChars.Length == 0,
                ftpErrMsg + "Movement part contained these "
                + " invalid characters: " + badChars);

            // sensibleness check
            if (lineU.Contains("LR") || lineU.Contains("RL")) {
                outputErrMsg("Warning: redundant rotation of LR or RL found in "
                    + lineOrig);
            }

            commands = cmds; 

            // check starting position

            var spPieces = sp.Split(' ');
            must<ArgumentException>(
                spPieces.Length == 3,
                ftpErrMsg + "Too many items for the starting position");

            var (xStr, yStr, direction) =
                    (spPieces[0], spPieces[1], spPieces[2]);

            // single digits?
            var digits = "0123456789";
            must<FormatException>(
                (xStr.Length == 1) && digits.Contains(xStr[0]) &&
                (yStr.Length == 1) && digits.Contains(yStr[0]),
                ftpErrMsg + "x and y must be single digits");

            x = int.Parse(xStr);
            y = int.Parse(yStr);
            must<ArgumentException>(
                xInRange(x) && yInRange(y),
                ftpErrMsg + $"x or y are invalid, are: {xStr}, {yStr}");

            var validDirections = "NSEW";
            must<ArgumentException>(
                (direction.Length == 1) 
                        && validDirections.Contains(direction),
                ftpErrMsg + 
                "Invalid direction, must be one of: " + validDirections);

            initDir = direction[0];

            // roundtrip and we should get back to our original
            // string (ignoring case anyway). If this is wrong,
            // no chance of recovery.
            var rtrip = $"{x} {y} {initDir}|{commands}";
            must<InvariantFailure>(
                rtrip == lineU,
                ftpErrMsg
                + $"Round trip failed. Expected {lineU} got {rtrip}");

        }  // parseLine

    }  // RoverUtils




    public static class UnitTests {


        // Taken from LDB.
        // 
        public static void ExpectException<Excptn>(
                        Action act, string msg)
                where Excptn : Exception, new() {

            try {
                    suppressOutput = true;
                    try { act(); } finally { suppressOutput = false; }

                    // Act should have blown up. If we get here,
                    // it's an error
                    quit("Failed to except in ExpectException "
                        + nl + "msg: " + msg
                        + nl + "Excepton expected was: "
                        + (new Excptn()).GetType().FullName ?? "(unknown)");
            } catch (Excptn) {
                // it's what we want, just suppress it
            } catch (Exception e) {
                Excptn tmpJustToGetType = new Excptn();
                var msg2 = "Expected exception of type: "
                    + $"'{tmpJustToGetType.GetType()}'"
                    + nl + $"but got type: '{e.GetType().ToString()}'"
                    + nl + $"with message: '{e.Message}'";
                quit(msg2);
            }
        }


        public static void run() {
            // A few sample unit tests

            {
                // expected parse failures should except
                int x, y;
                char initDir;
                string commands;                

                Action act = () =>
                    parseLine("2 2 NLLLLL", // no pipe
                        out x, out y, out initDir, out commands);
                ExpectException<ArgumentException>(
                    act, "Pipe char missing");


                act = () =>
                    parseLine("2 23 N|LLLLL", // multi-digit y loc
                        out x, out y, out initDir, out commands);
                // note different exception
                ExpectException<FormatException>(
                    act, "multi-digit co-ordinate");
            }


            {
                // rover checks

                var map1 = new Map();

                // walk it around the perimeter, should not except
                suppressOutput = true;                  
                var s1 = "0 0 E|MMMMMLMMMMMLMMMMMLMMMMM";
                var r1 = Rover.roverFromStr(s1);
                r1.go(0, map1);
                r1.mustBeAt(0, 0); // should be where it started
                suppressOutput = false;


                // walk it beyond the perimeter, should except
                Action act1 = () => {
                    var s2 = "5 5 E|M";
                    var r2 = Rover.roverFromStr(s2);
                    r2.go(0, map1);
                };
                ExpectException<ArgumentOutOfRangeException>(
                                        act1, "rover left grid");



                // walk entire map and check it's totally covered

                var map3 = new Map();
                // check full traversal assert works, I've got
                // that logic wrong before
                ExpectException<MapNotFullyTraversed>(
                    () => map3.mustBeFullyTraversed(),
                    "map is totally un-traversed");

                suppressOutput = true;
                var m5 = "MMMMM";
                var s3 = "0 0 E|" + m5 + 
                            "LML" + m5 +  "RMR" + m5 + 
                            "LML" + m5 + "RMR" + m5 + 
                            "LML" + m5;
                var r3 = Rover.roverFromStr(s3);
                r3.go(0, map3);
                suppressOutput = false;

                map3.mustBeFullyTraversed();


            }

        }

    }
}