### Introduction

![github-780x440-2-preview](https://github.com/user-attachments/assets/1df99fbf-d793-405b-a2c3-312d5d5808b6)

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

### Usage

To get a position evaluation, you need to click the 'Advice' button in the status bar or press F1 during your move.

![0126-1](https://github.com/user-attachments/assets/7df7de43-6aca-4b2b-98ea-9acbad298268)

By default, the best move found in the current position is recommended (on the left). Other possible moves are displayed as colored bars. The better the move, the greener it is. Neutral moves are gray. Moves that worsen the position are red (they are on the right). You can hover the mouse over any move.

![0126-2](https://github.com/user-attachments/assets/87e467a2-63cc-46d8-8c5e-ff448c1e0039)

The worst moves, which lead to checkmate, are colored in maroon.

![0126-3](https://github.com/user-attachments/assets/936fedfc-b02d-42fb-897d-52f32e6ef0b2)

You can not only hover the mouse cursor but also click on any move.

![0126-4](https://github.com/user-attachments/assets/643e15fa-27cc-40d4-bae2-a95839399183)

It will look like this:

![0126-5](https://github.com/user-attachments/assets/2b3e02de-497e-4efa-8ffe-e00cbd7f692d)

The advisor remembers the evaluation of the move (in this case, +1.04) and from that point on will recommend moves close to that evaluation. This mechanism allows you to adjust the strength of play. That is, to play at roughly the same level as your opponent. Or stronger, or weaker — depending on which moves you click.

You can also adjust the desired game strength at any time using the round buttons up and down — from "MAX" (choosing only the best move) to "MIN" (choosing the worst one). For example, you selected "-3.00":

![0126-7](https://github.com/user-attachments/assets/63159988-b597-4dc2-9a31-0a030d3832d0)

Only the best found move:

![0126-9](https://github.com/user-attachments/assets/f10e878f-1a9b-430b-aba6-9c57557b90d0)

Or the worst one:

![0126-8](https://github.com/user-attachments/assets/47f734f2-a867-4d05-b634-6a7b3a6f08ff)

The selected game strength is saved in the user profile (for example, in C:\Users\wmlab\AppData\Local\Chezzz\).

The file **openings.csv** contains opening positions and their names. If the move is known in opening theory, its name or the symbol "B" is added

![0202-1](https://github.com/user-attachments/assets/9839323e-9148-4b97-a9a6-3fe532e79708)

The advisor supports both popular chess websites. You can switch between them at any time.

![0124-8](https://github.com/user-attachments/assets/b76ccd3e-774b-4207-8470-babc7be6a4b0)

You can play either anonymously or in your account. All modes are supported — playing against people, bots, solving puzzles, and studies.

### Configuring

The advisor itself is a portable application. It can be downloaded from **Releases** and unpacked into any folder. However, it requires Stockfish to work. Stockfish is free; download it from [here](https://stockfishchess.org/download/), place it in any folder (you can put it directly in the advisor's folder), and specify the full path to it in the **App.config** file

```xml
<appSettings>
	<add key="StockFishPath" value="D:\Users\Murad\StockFish\stockfish-windows-x86-64-avx2.exe" />
</appSettings>
```

### Previous UI

Over the course of a month, I tested several interface options on myself, from a simple status bar to the current version, which shows all possible moves with color coding. I found the latter option to be the most convenient and interesting. It allows you to avoid giving yourself away by only making the best moves and beating everyone, but instead to play at a level comparable to your opponent, slightly better or slightly worse, demonstrating natural, imperfect play. However, nothing stops you from crushing anyone with 'green' moves — it's all in your hands!

The first version:

![header](https://github.com/user-attachments/assets/f506c2b2-9ea7-4f0a-8e93-1db3b4a4fa20)

The improved version:

![header-github](https://github.com/user-attachments/assets/cf690356-ed15-4c1e-8a49-4046f03513ab)

Added the three best move options:

![header-github-2](https://github.com/user-attachments/assets/456cf667-b8f1-4582-af2e-c2498a1d1714)

I changed the way the output is displayed, making it more compact, and simplified the settings (it's no longer possible to select the Elo rating). Now, up to three options are presented in one line: the best move, an average move, and a poor move — though, whenever possible, not a completely losing one. By balancing between the three options, you can either play to win, steer the game toward a draw, or play at a level roughly equal to your opponent's:

![image](https://github.com/user-attachments/assets/ad306d72-63e4-4c08-9592-ee9ec8c62101)

I changed the output format again. Now the 'advice' botton shows all possible moves divided into four categories: winning moves (green), moves that slightly improve the position, moves that slightly worsen the position (gray), and moves that lead to a loss (red). The categories are presented as dropdown lists and sorted in descending order of score:

![image](https://github.com/user-attachments/assets/8e34887f-d712-47ba-91c6-d0c24e1c40e7)

I also experimented with user controls that allowed scrolling through the list of possible moves, but found this option cumbersome and unsuccessful. Eventually, I arrived at the current version, which shows all possible moves and remembers the playing level.

The second-to-last version, still without the game level selection on the left:

![0124-1](https://github.com/user-attachments/assets/367a90c6-230a-451b-b502-ee590b60c1c1)

### Links
* Stockfish engine download: [(https://stockfishchess.org/download/)](https://stockfishchess.org/download/)
* UCI protocol: [https://github.com/official-stockfish/Stockfish/wiki/UCI-&-Commands](https://github.com/official-stockfish/Stockfish/wiki/UCI-&-Commands)
* FEN notation: [https://www.chess.com/terms/fen-chess](https://www.chess.com/terms/fen-chess)
