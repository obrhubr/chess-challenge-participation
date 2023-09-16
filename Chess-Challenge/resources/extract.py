import chess.pgn
import json
import re

pgn = open("test.pgn")
fens = []

while True:
    mygame = chess.pgn.read_game(pgn)
    if mygame is None:
        break

    stockfishColor = mygame.headers["White"].__contains__("Stockfish")
    print(mygame.headers["White"] + mygame.headers["Black"] + mygame.headers["Round"])
    while mygame.next():
        if mygame.turn() == stockfishColor:
            mygame = mygame.next()
            continue

        board = mygame.board()
        if board.is_checkmate():
            continue
        
        p = re.compile(r"((\+|-)[0-9].+?(?=\/))")

        eval = p.search(mygame.comment)
        if eval is not None:
            eval = eval.group(0)
            fens += [{ "fen": board.fen(), "eval": float(eval) }]
        
        mygame = mygame.next()

json_object = json.dumps({"positions": fens}, indent=4)
 
# Writing to sample.json
with open("analysis.json", "w") as outfile:
    outfile.write(json_object)