### Introduction
![header-github-3](https://github.com/user-attachments/assets/59ab55ad-4ad0-4a12-ad2a-9deb080bdd72)


I really love playing chess. I've been playing 10-minute games on chess.com for a long time. But my rating is stuck around 1500. And it's all because I often 'blunder'. What if there was an advisor nearby, helping me avoid those silly blunders... What if I could integrate a chess engine to give suggestions in tough positions? It's not fair play, but using bots of any rating for training is allowed. So the idea is to set up the engine, somehow read the chess position from the browser, send it to the engine, and get a response in a reasonable time.

### How it works
This is a simple WPF application with a single window and a **Microsoft.Web.WebView2** web control, running Stockfish as a child process and interacting with it through console input-output streams. In the current implementation, the board at chess.com is encoded as a set of <div> elements. Something like that:
```xml
<div class="piece bb square-55" style=""></div>
<div class="piece square-78 bk" style=""></div>
<div class="piece square-68 br" style=""></div>
...
<div class="piece square-61 wk" style=""></div>
<div class="element-pool" style=""></div>
<div class="piece wq square-41" style=""></div>
<div class="element-pool" style=""></div>
```
The piece class always starts with 'piece', the first character of the two-letter class is always 'w' or 'b' (for white or black piece), and the second character represents the piece itself ('r' for rook, 'p' for pawn, 'q' for queen, etc.), while 'square-XY' indicates the square. 11 corresponds to a1, and 88 corresponds to h8. However, the board can be flipped if we are playing as black. This is controlled by another element, a bit earlier:
```xml
<wc-chess-board class="board">
<wc-chess-board class="board flipped">
```
My advisor requires free Stockfish engine. I downloaded the binary version **stockfish-windows-x86-64-avx2.exe**. It doesn't require installation and can be placed in any folder. When launched, you will see an empty console window.

![stockfish_console](https://github.com/user-attachments/assets/354c3b3e-eb39-4d86-bc0c-7bd097df0b65)

We always start the dialogue with the '**uci**' command. The engine provides information about itself and ends the output with the '**uciok**' marker word. Now we can set the engine's parameters. Why? By default, the engine operates as a weak player with a minimal rating. I discovered this while playing test games with bots. The 'genius fish' made clearly weak moves and blundered pieces even worse than I did. The command '**setoption name UCI_Elo value XXXX**' sets the playing strength, where XXXX is a rating from 1320 to 3190 (for reference, grandmasters have a rating around 2600–2800). Next, it's recommended to specify the number of threads to improve performance (by default, one thread is used) with the command '**setoption name Threads value XXXX**'.

Once the settings are done, I send the '**ucinewgame**' command to indicate a new game. Next, we need to provide the position for analysis using '**position fen XXXX**', where XXXX is the encoded position in FEN format. Then, we ask the engine to accept it with the '**isready**' command. If everything is fine, the engine will respond with '**readyok**'. With the '**eval**' command, I get an approximate evaluation of the position (if it's positive, we have an advantage). After that, we just need to start the analysis with the '**go**' command and parameters (there are many – thinking time, search depth, number of positions to analyze, etc.). The engine then starts analyzing variations (outputting a lot of interesting information to the console, such as the evaluation of the current position), and it ends with the '**bestmove XXXX**' message (the best move found). This is the move I will show in the status bar. Then, the thread and child process can be closed.

I started exploring Stockfish’s features and added the display of three move suggestions with evaluations ('**setoption name MultiPV value 3**').

![estimation](https://github.com/user-attachments/assets/1a3c431b-52ff-41af-a051-11d3a2547e8e)

### Links
* Stockfish engine download: [(https://stockfishchess.org/download/)](https://stockfishchess.org/download/)
* UCI protocol: [https://github.com/official-stockfish/Stockfish/wiki/UCI-&-Commands](https://github.com/official-stockfish/Stockfish/wiki/UCI-&-Commands)
* FEN notation: [https://www.chess.com/terms/fen-chess](https://www.chess.com/terms/fen-chess)
