# Previous UI

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

Improved version with the option to select relative game strength and display opening variations.

![0202-1](https://github.com/user-attachments/assets/2d2fdfb6-fedb-46a8-aa51-2dee3b121eef)


A funny experiment. Moves are sorted not by evaluation but by pieces. They make decent hints to keep you from dozing off, but playing is noticeably harder.

![image](https://github.com/user-attachments/assets/c1ab15db-511a-4c53-beae-3ba9ef006aa8)

