# Chess Challenge

This is my entry for Sebastian Lague's [competition](https://www.youtube.com/watch?v=iScy18pVR58) that imposes an arbitrary limit on the length of your engines code. To be exact every participant has a maximum of 1024 tokens at his disposal to craft the best chess bot they can. Sounds like a lot, or extremely little depending of your conception of tokens. In this case a token is the smallest unit the compiler can see, with some exceptions. Therefore your 28 character Java like variable `ChessEngineBotThinkingBoardGamePiecePlayerMoverFactory` will only count as a single token, while `{` also counts as one. You also cannot load external files or use certain functions to extract variable names, which is enforced by limiting the allowed namespaces.

I wanted to submit a bot which uses an innovative technique to try and win the competition (not by creating the best bot but rather by creating the most innovative bot). To that end I started by implementing a basic chess bot, using all the tried and tested techniques (Minimax, quiescence search, search extensions and transposition tables).

This did sadly not work out, but I learned a lot along the way.

In this article:
 - [Implementing and debugging a chess engine](#Building-a-chess-computer)
 - [Trying to innovate and failing (using a Genetic Algorithm)](#Fine-tuning-the-evaluation-using-a-Genetic-Algorithm)
 - [Trying again and failing again (using statistics)](#Using-Statistics-to-create-a-perfect-evaluation-function)

## Building a chess computer

### Minimax

### Building a tool to debug the bot

### Quiescence Search and Search Extensions

### Time Management and Early Exit

## Fine tuning the evaluation using a Genetic Algorithm

This limitation, you would assume, hampers the addition of complex evaluation functions, but also prevents the implementation of any neural networks or similar structures necessary for chess engines of the second type described above. 

My first instinct therefore was to do something akin to knowledge distillation, inspired by the NNUE of Stockfish. It uses the current board as an input and by applying the weights and biases of the net it comes up with a number. If, instead of using a complex network, we used a single 64x64 grid of values for each type of piece, we could distill the game knowledge of powerful engines into a very small number of bytes. This evaluation function could then still be “plug and play” with a traditional Minimax engine.

![Visual illustration of the bitboards]()

### The Idea

The idea is very simple at it’s core. We would assigne a value to every square of the board a piece could stand on, and do that for every type of piece. To evaluate a position, we would only need to add the value of the squares every piece stands on. For example, a pawn is worth more, the further it moves up the board and the closer it comes to becoming a queen. Therefore we would assigne higher values to the squares further up the board, up to nearly the value of a queen on the seventh rank.

![Illustration of pawn values]()

![Code block of piece value bitboards]()

For the implementation, we would need to save the values of each square to an array. When evaluating, we get back a bitboard representing the different pieces on the board as an array, and use a bitwise and operation to multiply it and the “model” together (I will refer to a the boards with values for each square that the genetic algorithm outputs as a model from here on out). The result would be a board with only the values of the squares left, that have a piece currently standing on them. Summing the values of the 6 different boards (6 piece types: pawn, knight, bishop, rook, queen, king) together for each player and substracting them gives us a value in the format of the classic stockfish evaluation: `-1.5` for example to represent black winning by a certain margin, `+7.9` to represent white having a very powerful advantage.

### Implementing the genetic algorithm and training

The question that remains is that of setting the value for each of the squares of the boards. Here, a tried and tested technique of machine learning seemed perfect: genetic algorithms. The issue with traditional techniques of training machines in this context is that there are no nodes and edges to tune the values of using back and forward propagation.

Genetic algorithms on the contrary don’t use these more sophisticated techniques, and just mutate the values of the boards randomly between generations, always selecting the best of the current generation. Our training would therefore look something like this: we instantiate the model with randomly selected numbers between some boundary. We then evaluate these “weights” to assign a fitness to each and at the end, we select the very best. We then slightly but still randomly mutate the values to create 500 or more children and assign a fitness to each again. 

![Illustration genetic algorithm]()

We repeat this for a few hundred generations and we would be left with a single model that has been optimized for the fitness function. 

Now we have the issue of defining an appropriate fitness function, to make sure we really chose the models that have the best chance of producing accurate evaluations. To do this I downloaded a large number of lichess and chess engine games from the internet in PGN format. PGN stands for “portable game notation” and it encodes chess games into a series of moves with a bit of metadata.

![PGN game  with evaluation]()

I then extracted the evaluation for each position and stored these for training, resulting in a few hundredths of thousands of lines of chess positions. This seemingly random string is actually a [FEN](https://de.wikipedia.org/wiki/Forsyth-Edwards-Notation) string, which stores the position of each chess pieces, therefore encoding the board. Paired with the evaluation that a powerful chess engine assigned to it, this was the perfect data set to train my own engine.

![FEN position with eval]()

The fitness function would consist of evaluating a position with the values of the boards that the current child - generated using the genetic algorithm - has and taking the difference of that and the actual evaluation of the position. To get the best child in a generation we would then take the one with the lowest overall score, indicating that it was the best at estimating the actual evaluation.

![Illustration of training and fitness]()

### Disappointing results

My enthusiasm for this idea quickly ebbed away very quickly once I realised how long training would take and also how bad the first dozen generations really were.

Plugging in the values of the best models produced mediocre results, even against very weak opponents.

![Losing bot with the GA]()

I quickly realised the issue with my approach: distilling the knowledge stored in the [NNUE](https://tests.stockfishchess.org/nns), which is pretty small but still about 50MBs large, into only 24576 values was too much. The loss of context that my implementation produced was crippling it’s ability, because the different boards for each piece that make up the model do not consider the position relative to other pieces. Moving the queen to c6 would be evaluated as equally good, whether it blundered the queen to a pawn or delivered a beautiful checkmate.

## Using Statistics to create a perfect evaluation function

### The Idea

At the same time I also realised the futility of my genetic algorithm, which just overcomplicated the problem further. The nature of my fitness function and the genetic algorithm was to get the median evaluation for each square for each time a piece stood on it. In other words, the optimal solution, or the model with the best possible fitness, has weights for each square that are the median of the evaluations the training boards had when there was a piece standing on that specific square, because the fitness function would then find the lowest total difference between the expected evaluation (specified for each position in the training data) of a board, and the one provided by the model.

To illustrate the idea, consider a training set consisting of a single data point, a single FEN string associated to a single evaluation.

![Board and FEN string of the example]()

There is (although impossible in chess) a single white pawn standing on `a7` rank of the board. The evaluation is very high, at `+100.0` let’s say. To get the best fitness, the model would assign the value `+100.0` to the square `a7`.

If we were to add a second and third data point, one where that same position with a pawn standing on `7a` was evaluated at `+30.0`  and again at `-100.0`, to get the best fitness we would take the median of the three evaluations which results in a value of `30.0` for the square `a7` of the first board (representing the pawn values) of the model.

Why? Because when evaluating fitness we would first get the difference of the actual evaluation and our models evaluation for the first position, which would be `70.0` in this case, `0.0`for the second position and `130.0` for the third, for a total fitness of `200.0`. If we were to choose a different value for the square `a7`, for example `+10.0` - the median of the three evaluations - we would find that the total fitness is `220.0`.

### Implementing the statistical evaluation

### Disappointing results