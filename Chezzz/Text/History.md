# What is this even about?

This is a two-month story about the creation of a chess advisor to assist in playing on chess.com and lichess.org. It is a small Windows application that requires [Stockfish](https://stockfishchess.org/download/) to function. You need to download it and place it in any folder. The application was developed for personal enjoyment in my free time. It is free to use, and the source code is available on [github](https://github.com/wmlabtx/chezzz). Feel free to use it, modify it, or incorporate ideas into your own projects. I would appreciate it if you could credit me in the process.
# How It All Began

I really love playing chess. I constantly play ten-minute games on chess.com. But I have a problem— a tunnel vision. I see one move and fixate on it. Because of this, I miss a lot of opportunities and overlook pieces. My rating never rises above 1500. If only I had an advisor nearby who would stop me when I make a blunder...

I've long thought about looking into Stockfish, but I assumed it was difficult to integrate. In December, I read an article about it, which mentioned that it supports console input-output perfectly, and I got excited about the idea of making it my assistant.

On Christmas Eve, we weren't working, and I started considering how to integrate it with chess.com. Stockfish can be launched as a child process, you can pass an encoded position and settings (including how much time it has to think) to the console, and... read the response. But how to get the position?

For a while, I considered options with machine vision. But on chess.com, there are dozens of board and piece options in the settings. Plus, the size and position of the board can vary. Then I started thinking about connecting to the browser. But there are many browsers, plus the hassle of reading another process... A browser add-on? I have no experience writing them. The solution came to me — I would embed the browser in my application. Then there would be no problem reading the current web page.

The final version looks like this — a simple WPF application with a single window and a web control, launching StockFish as a child process and interacting with it through console input-output streams. In five minutes, I create a new WPF project in Visual Studio, add a WebBrowser as the main control, set its homepage to chess.com, launch it... and JavaScript errors, nothing works.
# The built-in browser

The ancient WebBrowser WPF control still relies on Internet Explorer libraries, which are incompatible with modern web pages. Something newer is needed — Chromium or Edge (which is also Chromium, but with a different shell). There are libraries available for both options. My main browser is Microsoft Edge, so I install the component from Microsoft itself — Microsoft.Web.WebView2, via NuGet.

![Pasted image 20250302175652](https://github.com/user-attachments/assets/80cd9583-a2d9-4d35-be06-f1ff530f3a8c)

Now everything is working. I even logged into my account, closed the app, launched it again — and I'm still logged into my account. So cookies and sessions are supported. Wonderful. However, working with the DOM like before won't be possible. In WebView2, you can't just access the HtmlDocument and DOM like in the good old days. The world has changed, pages are dynamic, so we view the content differently.
```C#
const string script = "document.documentElement.outerHTML";
var result = await WebBrowser.CoreWebView2.ExecuteScriptAsync(script);
var decodedHtml = Regex.Unescape(result.Trim('"'));
```
It remains to figure out how to extract the board and pieces from the HTML page.

# Chess board parsing

In the current implementation, the chess.com board is encoded with a set of divs.
```html
<div class="piece bb square-55" style=""></div>
<div class="piece square-78 bk" style=""></div>
<div class="piece square-68 br" style=""></div>
...
<div class="piece square-61 wk" style=""></div>
<div class="element-pool" style=""></div>
<div class="piece wq square-41" style=""></div>
<div class="element-pool" style=""></div>
```
The class of a piece always starts with "piece", the first character of the two-letter class is always "w" or "b" (white or black piece), the second is the piece itself ("r" for rook, "p" for pawn, "q" for queen, etc.), and "square-XY" indicates the square. 11 is a1, 88 is h8. However, the board can be flipped if we are playing as black. This is determined by another element, slightly earlier.
```html
<wc-chess-board class="board flipped">
```
The presence of a `flipped` class suggests that the board is turned over. You can extract the positions of all pieces with a simple regular expression. It turned out to be more difficult to convey them to Stockfish.

# Stockfish interaction

I downloaded the binary version **stockfish-windows-x86-64-avx2.exe** [from here](https://stockfishchess.org/download/). It doesn't require installation and can be placed in any folder. When launched, you see an empty console window. 

Always start the dialogue with the command "**uci**". The engine provides information about itself and ends the output with the marker word "**uciok**". Next, it's advisable to specify the number of threads to improve performance (by default, one thread is used) with the command "**setoption name Threads value XXXX**". After finishing the settings, send the command "**ucinewgame**", which means a new game. Then, you need to specify the position for analysis, "**position fen XXXX**", where XXXX is the encoded position. I will explain the format below. And request to accept it with the command "**isready**". If everything is in order, the engine will respond with "**readyok**". Finally, we need to start the analysis with the command **go** with parameters. I used "**go movetime XXXX**", where XXXX is the number of milliseconds given for thinking.

![Pasted image 20250302185139](https://github.com/user-attachments/assets/d6ffd838-d670-4b97-b873-bd614c85d87e)

After this, the engine starts evaluating options (dumping a lot of interesting information into the console, such as the assessment of the current position), and it ends with the message "**bestmove XXXX**" (the best move found). This is what I will display in the status bar. Then the stream and the child process can be closed. What is the heck FEN encoded string?

# `"rnbqkbnr/.../RNBQKBNR w KQkq - 0 1"`???

A FEN position consists of a description of eight ranks, separated by slashes. For example, the third rank looks like this: "2P2N2". "2" means first two empty squares, then a white pawn ("P" in uppercase), then again two empty squares, a white knight ("N" in uppercase), and then again two empty squares. Black pieces are written in lowercase.

Then comes an information block of six fields. Why are they needed if the position is clear? In fact, it's not. In a position taken from the middle of a game, additional information is missing. The first field "**w**" indicates it's white's turn, or "**b**" for black's turn. Then we must list possible castling (there are four — "**KQkq**", two for each side) or "**-**" if castling is unavailable. For example, a king may move before castling and later return to its square. The position will look "innocent," but the possibility of castling is already lost since the king has moved. Next, we must indicate if there are *en passant* captures, which is not always clear from the position. The penultimate field is the half move clock. This is needed to determine a draw if there have been no pawn movements or captures for a long time. The last field indicates the number of full moves (i.e., a move for both black and white). To be honest, it's unclear where to get them from. A simple board with pieces doesn't provide such information. In the first version, I always state "**- - 0 1**". This is generally inaccurate, castling will never be offered, but it's reliable and suitable for the first version. Later, I corrected this (opening the gates of hell). For details on FEN, I refer to the [FEN description](https://www.chess.com/terms/fen-chess). 

So, we read the pieces from the page, encoded the position in FEN (for now, just the pieces themselves, without additional information), passed it to Stockfish, received "**bestmove XXXX**", and displayed it in the status bar.

![header](https://github.com/user-attachments/assets/2a0636e2-a9a9-4c4d-b54e-3807bc5634f3)

Is the task solved? No.

# Stockfish plays TOO well

By making only the best moves, I will win against everyone, including the world champion, and will justly receive an account ban. That's not what I need at all. I need an advisor who will protect me from foolish moves and won't suggest playing much stronger or weaker than the opponent. 

I had to delve into the UCI commands. Stockfish indeed has a setting for adjusting playing strength (either by levels 1-20 or by Elo rating (1320-3190)), but in the current version, it has a rather straightforward algorithm (it's easily readable in the source code), which sometimes chooses random, absurd, nonsensical moves. For those interested, here is the beginning of this function.
```C
Skill(int skill_level, int uci_elo) {
	if (uci_elo) {
        double e = double(uci_elo - LowestElo) / (HighestElo - LowestElo);
        level = std::clamp((((37.2473 * e - 40.8525) * e + 22.2943) * e - 0.311438), 0.0, 19.0);
    }
    else {
        level = double(skill_level);
    }
    ...
```
To begin with, I discovered the setting "**set option name MultiPV value N**". It instructs Stockfish to return not only the best move but also the top N moves, sorted in descending order. Additionally, for each move, you can return the WDL ("win-draw-loss") statistics "**setoption name UCI_ShowWDL value true**". These are three numbers that add up to one hundred, for example, "30-60-10", which allows you to roughly calculate the probability of winning or losing. Shall we try the top three moves to start?

![header-github-2](https://github.com/user-attachments/assets/891f0a26-4678-4437-8727-8fa51f2fb15f)

But, only three possible moves don't make me happy.

# I Want to See All the Moves

Three options of varying strength are better than one. But if we already know the evaluation of each move, let's specify the desired level in the settings, for example, "+3.00" (an advantage of a extra bishop) or "-1.00" (let the opponent have an extra pawn). At the same time, let's color the move in different shades - from red to green. And instead of showing the top three moves, let's show them all!

![0126-2](https://github.com/user-attachments/assets/01230f76-668c-4437-af14-245888cf862b)

To avoid being annoyed by ridiculous opening moves, I compiled a book of openings from various sources into a single .csv file (a relatively small amount, about 3000 positions). If a move is present in theory, its name can be displayed.

![Pasted image 20250303081904](https://github.com/user-attachments/assets/708552f5-76c8-4f49-83be-cfc4df355cba)

# Why only chess.com?

At this point, I bragged on one of the platforms. One of the commenters mentioned that chess.com already has a hint option when playing with a bot. And real pros are on lichess.org. There's no such option there. I had to figure out how the board is coded there.
```html
<cg-board>
<piece class="black rook" style="transform: translate(0px, 0px);"></piece>
<piece class="black bishop" style="transform: translate(174px, 0px);"></piece>
<piece class="black queen" style="transform: translate(174px, 87px);"></piece>
<piece class="black king" style="transform: translate(522px, 0px);"></piece>
```
The logic remains the same in other respects. You can choose between the sites at any time.

![0124-8](https://github.com/user-attachments/assets/024e3348-d70d-4fe1-8054-6aa6f51d4f1b)

# I need an advisor, not a dictator

It seems everything is fine, but it didn't turn out the way I intended. I decided to make the moves myself, but I need a quick answer on whether the move is a blunder and simply losing. Searching for my move in the multicolored stripes is exhausting. Therefore, I decided to group the moves by pieces.
```C#
var groups = _moves
	.Values
	.OrderByDescending(move => move.Score)
	.GroupBy(move => move.FirstMove[..2])
	.Select(group => group.ToArray())
	.OrderByDescending(list => list.First().Score)
	.ToArray();
```
![Pasted image 20250303113537](https://github.com/user-attachments/assets/42460e43-036c-4b5f-890e-bd135f21f71d)

Looking for a move this way is faster, but the design is overloaded; there are too many repeating symbols. So I decided to discard them. After all, the first half of the move for the piece is the same.

Thus, we have approached the current interface.

![0 1 2-1](https://github.com/user-attachments/assets/2fb4aac8-b27f-42d0-809d-4fd6e4da6b8e)

Moves that are closer to the required score (like +5.00 in the screenshot) are shown in full. The others are represented by bars. You can adjust the intensity of the game by selecting the score using the buttons to the right of the indicator (the selected moves will change dynamically). Alternatively, you can simply click on an interesting move, and the intensity will adjust accordingly.

![0 1 2-3](https://github.com/user-attachments/assets/b9a0a59a-753c-4790-bd6f-845291a29b52)

Here we decided to slowly lose by setting the level to "-1.50". It's no surprise that all the moves are red... Except for one green one. It's shown as a green optimistic bar, but by hovering the mouse cursor over it, you can read that in a losing position, we have the opportunity to checkmate in four moves.

It's just that reading moves on strips and translating "h5f7" onto the board is still tiring. If only it were possible to draw the move directly on the board...

# Drawing a move

Initially, I thought it would be easy to place a Canvas in front of the WebBrowser element, get the board's offset relative to the top-left corner using a JS function, and then draw anything on the board, be it lines or text. But it didn't work. WebView2 uses hardware acceleration and DirectComposition to render its content, which creates complexities when integrating with the traditional WPF rendering system. I only managed to overlay the control with another window, without a border and title, and track all the movements of the main window... Quite a nightmare, and it all worked unstably.

Then I decided to implement my elements on the board just like chess.com does. That is, we draw an arrow as a polygon and insert it directly into the page code using JS.
```C#
var x1 = (src[0] - 'a') * 12.5 + 6.25;
var x2 = (dst[0] - 'a') * 12.5 + 6.25;
var y1 = ('8' - src[1]) * 12.5 + 6.25;
var y2 = ('8' - dst[1]) * 12.5 + 6.25;
if (!isWhite) {
	x1 = 100.0 - x1;
	x2 = 100.0 - x2;
	y1 = 100.0 - y1;
	y2 = 100.0 - y2;
}
var dx = x2 - x1;
var dy = y1 - y2;
var angle = Math.Round(Math.Atan2(dx, dy) * (180.0 / Math.PI), 2);
var length = Math.Round(Math.Sqrt(dx * dx + dy * dy), 2);
const double headRadius = 1.5;
var point1X = x1 + headRadius;
var point2Y = y1 - length + headRadius * 2;
var point3X = x1 + headRadius * 2;
var point4Y = y1 - length;
var point5X = x1 - headRadius * 2;
var point6X = x1 - headRadius;
var points = $"{point1X},{y1} {point1X},{point2Y} {point3X},{point2Y} {x1},{point4Y} {point5X},{point2Y} {point6X},{point2Y} {point6X},{y1}";
var svgElement = $"<svg viewBox='0 0 100 100'><polygon transform='rotate({angle} {x1} {y1})' points='{points}' style='fill: rgb(255, 255, 0); opacity: 0.7;' /></svg>";
```
It turned out quite nicely.
![[0.1.2-6.png]]
There is one drawback — the arrow doesn't disappear on its own if the opponent makes a move. A mechanism is needed that automatically removes the arrow if there are changes on the board. For example, a MutationObserver. We add the arrow, enable the MutationObserver. It triggers (for instance, if we or the opponent makes a move) — the arrow is removed. In fact, the arrow disappears already during the move, as picking up a piece with the mouse is a change in the DOM.
```js
window._chessBoardObserver = new MutationObserver(function(mutations){{
  if(window._disableArrowObserver){{
    return;
  }}
  mutations.forEach(function(mutation){{
    if(mutation.type === 'childList' || mutation.type === 'attributes'){{
      removeArrow();
    }}
  }});
}});
```
# Unexpected pitfall

 What is bad, however, is that the FEN position is inaccurate in the final part, which I always have as "**KQkq - 0 1**". It's unclear whether there's an *en passant* pawn, whether the right to castle has been lost, how many moves have been made without pawn movements... without all this information, Stockfish will provide incorrect analysis in a certain percentage of positions. I didn't even anticipate how complex this minor task would turn out to be.

Initially, I tried to find the FEN of the current position in the chess.com page's code. It is indeed possible to obtain it by making a web request... but only when playing against a bot. When playing against a human, this option is unavailable. Most likely, this is intentional to make it difficult for third-party applications to analyze the position.

But on our page, we have a record of all previous moves. It is on both sites, chess.com and lichess.org, but in a different format.

![Pasted image 20250303200854](https://github.com/user-attachments/assets/9c858811-c864-4f8f-a394-8e82bd5c9930)

That is, theoretically, it is possible to start from the initial position and, by repeating all these moves, arrive at the current position. But in this case, we would know everything we need to construct the actual FEN: whether the kings or rooks have moved, how many half-moves have been made without pawn movement, whether there are *en passant* pawns...
```html
<i5z>8</i5z><kwdb class="">Nf3</kwdb><kwdb class="">Nc6</kwdb>
<i5z>9</i5z><kwdb class="">Be2</kwdb><kwdb class="">a6</kwdb>
<i5z>10</i5z><kwdb class="">Nbxd4</kwdb><kwdb class="">Bc5</kwdb>
<i5z>11</i5z><kwdb class="">c3</kwdb><kwdb class="">O-O</kwdb>
<i5z>12</i5z><kwdb class="">Bd3</kwdb><kwdb class="">Ne7</kwdb>
```
It seems simple - set up an 8x8 array with chars as pieces, move the symbols around... But for this, we need to know from which square the piece is moving and to where. In SAN notation, the concept of "from where" is absent. So translating a move from "Bd3" (SAN) to "c1d3" (UCI) is a non-trivial task, considering the ambiguities. This is when different pieces can move to the same square, and you need to analyze additional SAN symbols. And then there are castling, checks, and other intricacies. Unfortunately, Stockfish itself cannot translate SAN to UCI. You either need to write a chess engine yourself or use an external library.

I found a very good chess library - [Geras1mleo](https://github.com/Geras1mleo/Chess) (60 stars on GitHub plus one from me) and started studying the source code. Initially, I wanted to use it as is, but I was eager to modify some things to suit my needs. Besides, 80% of the code was unnecessary for me (printing, parsing FEN and PGN, converting UCI to SAN, move validity checking). On the other hand, there was no option to manually set up the pieces. And I need both options - setting up a position (if the move history is unavailable, for example, when solving studies) and restoring FEN from the move history.

Let me repeat, the library is excellent, and some nuances of FEN became clear after studying the source code. I used the code partially, even preserving the names of some functions. Nevertheless, it resulted in almost 1000 lines of code and writing unit tests. Adding the calculation of the exact FEN position in the browser took seven evenings of coding and debugging.

![0 1 2-4](https://github.com/user-attachments/assets/f6201788-f9f0-4091-9806-9a28829cf0e8)

By the way, FEN in the status bar can be highlighted with the mouse and copied. For example, for analysis by some program.

"**kq - 2 12**" here means that only black player can perform both their castlings ("kq"), there are no *en passant* captures ("-"), two half-moves have been made without pawn movements, and 12 full moves have passed since the start of the game. Now the Stockfish position analysis is 100% accurate.

The actual interface.

![0 1 1-8-p](https://github.com/user-attachments/assets/592371b3-479a-4212-804e-342537be82cb)

# Conclusion

 I made this thing (and I'm sharing it for free) for fun. I remind you that cheating is unfair and dishonest towards your opponent. On the other hand, this advisor allows you to "level" the strength of the game if the opponents are in different weight categories. The games become interesting. If the opponent has a rating of, for example, 1800, setting the strength to "+0.50" will allow you to play at a 1900-2000 rating. Of course, this should only be done with the permission and approval of the opponent. For training purposes, for example.
