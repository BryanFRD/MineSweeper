using System.Text.RegularExpressions;

const int MAX_WIDTH = 26, MAX_HEIGHT = 26, MIN_BOMBS = 5;
int width = 10, height = 10, bombs = 5, bombMarked = 0, caseRevealed = 0, maxCase = 0;

ConsoleColor[] colors = {ConsoleColor.DarkGreen, ConsoleColor.DarkYellow, ConsoleColor.DarkRed};

/*
'\0' = caché
b = bombe
v = bombe caché
n = bombe marqué
h = vide marqué
0-8 = affiché
*/

char[,] gameArray;

bool isPlaying = false;
Regex regex = new Regex(@"([a-zA-Z])(\d{1,})\s(a|b)");

AskToPlay();

void AskToPlay(){
    Console.Write("Voulez vous jouer ? (O | N) ");
    string userInput = Console.ReadLine();
    if(!String.IsNullOrWhiteSpace(userInput)){
        if(userInput.Equals("O", StringComparison.OrdinalIgnoreCase)){
            Play();
            return;
        } else if(userInput.Equals("N", StringComparison.OrdinalIgnoreCase)){
            return;
        }
    }
    Console.WriteLine("Erreur, veuillez reessayer !");
    AskToPlay();
}

void Play(){
    Console.WriteLine("Lancement de la partie.");
    isPlaying = true;
    
    AskPlayerGameSize();
    ShowGame();
    while(isPlaying){
        Match userInputMatch = AskPlayerCoordinate();
        
        int x = char.Parse(userInputMatch.Groups[1].Value) - 'a', y = int.Parse(userInputMatch.Groups[2].Value) - 1;
        string userAction = userInputMatch.Groups[3].Value;
        
        if(!(x >= 0 && x < width && y >= 0 && y < width)){
            Console.WriteLine($"Les coordonnées entrées ne sont pas valides !\nCoordonnées entrées: {x}:{y}.\nCoordonnées MAX: {width}:{height}.");
            continue;
        }
        RevealCaseAtCoordinate(x, y, userAction);
        
        ShowGame(!isPlaying);
    }
    AskToPlay();
}

void AskPlayerGameSize(){
    Console.Write("Veuillez saisir une taille de jeu et le nombre de bombes: (x y bombes) ");
    string userInput = Console.ReadLine();
    
    if(!String.IsNullOrWhiteSpace(userInput)){
        string[] userInputArgs = userInput.Split(" ");
        
        if(userInputArgs.Length == 3){
            try {
                int w = int.Parse(userInputArgs[0]), h = int.Parse(userInputArgs[1]), b = int.Parse(userInputArgs[2]);
                if(w >= 5 && w <= MAX_WIDTH && h >= 5 && h <= MAX_HEIGHT && b >= MIN_BOMBS && b <= (w*h)/2) {
                    width = w;
                    height = h;
                    bombs = b;
                    
                    maxCase = (width * height) - bombs;
                    
                    gameArray = new char[width, height];
                    
                    Random rnd = new Random();
                    for(int i = 0; i < bombs; i++){
                        int rx = rnd.Next(w), ry = rnd.Next(h);
                        gameArray[rx, ry] = 'v';
                    }
                    
                    Console.WriteLine($"Taille de jeu définie sur {width}:{height} avec {bombs} bombes.");
                    return;
                } else {
                    Console.WriteLine($"La taille n'est pas correcte ! Taille MAX : {MAX_WIDTH}:{MAX_HEIGHT} et {MIN_BOMBS} bombes MIN.");
                    AskPlayerGameSize();
                    return;
                }
            } catch {}
        }
    }
    Console.WriteLine("Erreur lors de la récupération de la taille de jeu");
    AskPlayerGameSize();
}

Match AskPlayerCoordinate(){
    Console.WriteLine("Entrez la case à afficher/bloquer: (A1 Afficher|Bloquer [a|b])");
    string userInput = Console.ReadLine().ToLower();
    
    if(!String.IsNullOrWhiteSpace(userInput)){
        if(regex.IsMatch(userInput)){
            return regex.Match(userInput);
        }
    }
    
    Console.WriteLine("Erreur lors de la récupération des coordonnées.");
    return AskPlayerCoordinate();
}

void ShowGame(bool revealBombs = false){    
    int c = 0;
    for(int y = -1; y <= height; y++){
        if(y == height)
            continue;
        
        for(int x = -1; x <= width; x++){
            if(x == width){
                Console.WriteLine();
                continue;
            }
            c++;
            if(x == -1){
                WriteWithColor(String.Format("{0:00}", y != -1 ? y+1 : "  "), ConsoleColor.DarkGray);
                continue;
            }
            if(y == -1){
                WriteWithColor(x != -1 ? " " + Convert.ToChar(x + 'A') : "  ", ConsoleColor.DarkGray);
                continue;
            }
            
            char currentChar = gameArray[x, y];
            
            int currentCharInt = -1;
            bool isInt = int.TryParse(currentChar.ToString(), out currentCharInt);
            
            if(revealBombs && (currentChar == 'v' || currentChar == 'b' || currentChar == 'n')){
                WriteWithColor("▓▓", ConsoleColor.Black, ConsoleColor.Red);
            } else if(isInt){
                WriteWithColor(" " + (currentCharInt == 0 ? " " : currentCharInt), c % 2 == 0 ? ConsoleColor.DarkGray : ConsoleColor.Gray, GetForegroundColorForNumber(currentCharInt));
            } else if(currentChar == 'b'){
                WriteWithColor(" B", c % 2 == 0 ? ConsoleColor.DarkGray : ConsoleColor.Gray);
            } else if(currentChar == 'n' || currentChar == 'h'){
                WriteWithColor(" F", c % 2 == 0 ? ConsoleColor.DarkGray : ConsoleColor.Gray);
            } else if(currentChar == '\0' || currentChar == 'v'){
                WriteWithColor("▓▓", c % 2 == 0 ? ConsoleColor.DarkGray : ConsoleColor.Gray);
            } else {
                WriteLineWithColor("Aucune conversion trouvé!", ConsoleColor.Black, ConsoleColor.Red);
            }
        }
    }
}

ConsoleColor GetForegroundColorForNumber(int num){
    if(num <= 0)
        return ConsoleColor.Gray;
    
    return colors[Math.Min(num - 1, colors.Length - 1)];
}

//TODO Faire en sorte que cette fonction gere l'affichage et le bloquage des cases
void RevealCaseAtCoordinate(int x, int y, string showOrBlock){
    if(x < 0 || x >= width || y < 0 || y >= height)
        return;
    if(!isPlaying)
        return;
    if(HasABomb(x, y)){
        if(showOrBlock == "b"){
            gameArray[x, y] = 'n';
            bombMarked++;
        } else {
            EndGame(false);
            return;
        }
    }
    
    char currentChar = gameArray[x, y];
    if(currentChar == '\0' || currentChar == 'h'){
        if(showOrBlock == "a"){
            int bombsAround = GetBombAround(x, y);
            gameArray[x, y] = char.Parse(bombsAround.ToString());
            caseRevealed++;
            
            if(bombsAround == 0){
                for(int i = -1; i <= 1; i++){
                    RevealCaseAtCoordinate(x + i, y, showOrBlock);
                    RevealCaseAtCoordinate(x, y + i, showOrBlock);
                }
            }
        } else {
            gameArray[x, y] = 'h';
        }
    }
    
    if(caseRevealed == maxCase && bombMarked == bombs){
        EndGame(true);
        return;
    }
}

int GetBombAround(int x, int y){
    int b = 0;
    
    for(int i = -1; i <= 1; i++){
        for(int j = -1; j <= 1; j++){
            if(i == 0 && j == 0)
                continue;
            
            if(x + i < 0 || x + i >= width || y + j < 0 || y + j >= height)
                continue;
            
            if(HasABomb(x + i, y + j))
                b++;
        }
    }
    return b;
}

bool HasABomb(int x, int y){
    if(x < 0 || x >= width || y < 0 || y >= height)
        return false;
    
    char charAtCoord = gameArray[x, y];
    return charAtCoord == 'v' || charAtCoord == 'b' || charAtCoord == 'n';
}

void EndGame(bool hasWon){
    if(hasWon){
        WriteLineWithColor("Vous avez gagné :)", ConsoleColor.Black, ConsoleColor.Green);
    } else {
        WriteLineWithColor("Vous avez perdu :(", ConsoleColor.Black, ConsoleColor.Red);
    }
    
    isPlaying = false;
}

void WriteWithColor(string text, ConsoleColor backgroundColor = ConsoleColor.Black, ConsoleColor foregroundColor = ConsoleColor.Gray){
    Console.BackgroundColor = backgroundColor;
    Console.ForegroundColor = foregroundColor;
    
    Console.Write(text);
    Console.ResetColor();
}

void WriteLineWithColor(string text, ConsoleColor backgroundColor = ConsoleColor.Black, ConsoleColor foregroundColor = ConsoleColor.Gray){
    Console.BackgroundColor = backgroundColor;
    Console.ForegroundColor = foregroundColor;
    
    Console.WriteLine(text);
    Console.ResetColor();
}