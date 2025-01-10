using UnityEngine;
using UnityEngine.InputSystem;
using System;
using DG.Tweening;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Collections.Generic;

public enum State {
    IDLE,
    SPINNING,
    STOPPING,

    SHOW_WINS
}

public class Pair<T, U>
{
    public T First { get; set; }
    public U Second { get; set; }
 
    public Pair(T first, U second) {
        this.First = first;
        this.Second = second;
    }
 
    public override string ToString() {
        return $"({First}, {Second})";
    }
};

public class Reelset : MonoBehaviour
{

    [SerializeField] float rotationTime;
    State state = State.IDLE;
    public Texture tex;
    GameObject[] objs;
    InputAction interactAction;

    List<List<string>> outcomeSymbols = new List<List<string>>();

    public int numRows = 3;
    public int numCols = 4;

    bool hasWin = false;

    float currRotation = 0.0f;

    Dictionary<int, List<int>> reelWeights;
    Dictionary<int, List<string>> reelSymbols;

    Dictionary<string,GameObject> symbolMap = new Dictionary<string,GameObject>();

    List<List<Pair<int,int>>> wins;
    List<string> winningSymbols = new List<string>();

    public UI ui;

    public int credits = 500;
    int creditsWon = 0;

    Dictionary<string,int> symbolCreditMap = new Dictionary<string, int>{
        {"Cherry" , 10},
        { "Seven" , 20},
        {"Bar" , 50},
        {"Bell" , 15},
        {"Lemon" , 8},
        {"Grape" , 12},
        {"Orange" , 10},
        {"Plum" , 25},
        {"Diamond" , 75},
        {"Star" , 100}
    };

    void Start()
    {
        setupMathmodel();
        interactAction = InputSystem.actions.FindAction("Interact");
        setReels();
       
    }

    // Update is called once per frame
    void Update()
    {
        handleInput();
        switch(state) {
            case State.SPINNING:
                break;
            case State.IDLE:
                break;
        }
        

        
    }


    void handleInput() {
        if (interactAction.WasPerformedThisFrame()) {

            switch (state) {
                case State.IDLE:
                    credits -= 15;
                    ui.updateMeter();

                    state = State.SPINNING;

                    transform.DORotate(new Vector3(currRotation+180.0f,0.0f,0.0f),1.0f)
                        .SetEase(Ease.InQuint)
                        .OnComplete(onMidSpinComplete);
                    currRotation += 180.0f;
                    
                    break;
            }
        }
    }

    void determineOutcome() {
        
        List<string> symbolArray = new List<string>();
        List<List<string>> outcomes = new List<List<string>>();
        for (int i = 0; i < numCols; i++) {
            outcomes.Add(new List<string>());
            List<int> weights = reelWeights[i+1];
            for (int j = 0; j < numRows; j++) {
                int symbolIndex = rollSymbolIndex(weights);
                string symbol = reelSymbols[i+1][symbolIndex];
                outcomes[i].Add(symbol);
                setSymbol(i,j,symbol);

            }
        }

        wins = CheckWaysToWin(outcomes);

        for (int i = 0; i < wins.Count; i++) {
            List<Pair<int,int>> win = wins[i];
            string symbol = winningSymbols[i];

            Dictionary<int,int>reelToSymbolCount = new Dictionary<int, int>();

            for (int j = 0; j < win.Count; j++) {
                Pair<int,int> coord = win[j];
                if (reelToSymbolCount.ContainsKey(coord.First)) {
                    reelToSymbolCount[coord.First] = reelToSymbolCount[coord.First] + 1;
                }
                else {
                    reelToSymbolCount[coord.First] = 1;
                }
                print("waysCount: " + reelToSymbolCount[coord.First].ToString());
            }

            int ways = 1;
            foreach (var val in reelToSymbolCount.Values) {
                ways *= val;
            }

            int creditsWonTmp = ways*symbolCreditMap[symbol];
            print(creditsWonTmp.ToString() + " Credits Won");
            creditsWon += creditsWonTmp;
        }

        credits += creditsWon;

        if (wins.Count > 0) {
            hasWin = true;
        }
        else{
            hasWin = false;

        }

        
    }

    void onMidSpinComplete() {
        determineOutcome();
        transform.DORotate(new Vector3(currRotation+180.0f,0.0f,0.0f),1.0f)
                        .SetEase(Ease.OutQuint)
                        .OnComplete(onSpinEnd);
        currRotation += 180.0f;
        if (currRotation >= 360.0f) {
            currRotation = 0.0f;
        }
        
    }

    void onSpinEnd() {

        if (hasWin) {
            state = State.SHOW_WINS;
            ui.updateMeter();
            showWins();
        }
        else {
            state = State.IDLE;
            creditsWon = 0;
        }
    }

    void showWins() {
        //print("Winning positions:");
        foreach (var win in wins)
        {
            foreach (var symbolSet in win) {
                //print(symbolSet);
                doFlash(symbolSet.First,symbolSet.Second);
            }
        }
    }

    // Populate the symbols onto the reels
    void setReels() {

        for (int i = 0; i < numRows; i++) {
            outcomeSymbols.Add(new List<string>());
            for (int j = 0; j < numCols; j++) {
                    outcomeSymbols[i].Add("");
            }
        }

        foreach (Transform child in transform)
        {

            GameObject o = child.gameObject;

            // Mesh m = o.GetComponent<Mesh>();
            Renderer r = o.GetComponent<Renderer>();

            if (o.tag != "Reelset") {
                //print("child name = " + child.name);
                symbolMap[child.name] = o;
                Texture t = Resources.Load<Texture2D>("Symbols/"+reelSymbols[1][rollSymbolIndex(reelWeights[1])]);

                r.material.SetTexture("_BaseMap",t);
            }
        }
    }

    int rollSymbolIndex(List<int> weights) {
        int result = 0;
        List<int> A = new List<int>();
        A.Add(weights[0]);

        for (int i = 1 ; i < weights.Count; i++) {
            A.Add(A[i-1] + weights[i]);
        }

        int r = UnityEngine.Random.Range(1,100);
        for (int j = 0; j < A.Count; j++) {
            int w = A[j];
            if (r <= w) {
                result = j;
                break;
            }
        }

        return result;
    }


    // load in paytable
    public void setupMathmodel() {

        XDocument slotMachine = new XDocument(XDocument.Load("Assets/Resources/Reels.xml"));

        // Create a dictionary to hold reel weights
        Dictionary<int, List<int>> reelWeightsTemp = new Dictionary<int, List<int>>();
        Dictionary<int, List<string>> reelSymbolsTemp = new Dictionary<int, List<string>>();
        
        // Iterate through each reel
        foreach (var reel in slotMachine.Descendants("Reel"))
        {
            // Get the reel ID
            int reelId = int.Parse(reel.Attribute("id").Value);

            // Extract weights of symbols into a list
            List<int> weights = reel.Descendants("Symbol")
                                    .Select(symbol => int.Parse(symbol.Attribute("weight").Value))
                                    .ToList();

            List<string> values = reel.Descendants("Symbol")
                                    .Select(symbol => symbol.Value)
                                    .ToList();

            // Add the weights to the dictionary
            reelWeightsTemp[reelId] = weights;
            reelSymbolsTemp[reelId] = values;
        }

        reelWeights = reelWeightsTemp;
        reelSymbols = reelSymbolsTemp;
    }

    public void setSymbol(int reel, int row, string symbolName) {
        string rowString = row.ToString();
        if (row < 10) {
            rowString = '0' + rowString;
        }
        string key = reel.ToString() + rowString;
        GameObject obj = symbolMap[key];
        Renderer r = obj.GetComponent<Renderer>();

        Texture t = Resources.Load<Texture2D>("Symbols/"+symbolName);

        r.material.SetTexture("_BaseMap",t);
    }

    public void doFlash(int reel, int row) {
        string rowString = row.ToString();
        if (row < 10) {
            rowString = '0' + rowString;
        }
        string key = reel.ToString() + rowString;
        GameObject obj = symbolMap[key];
        Renderer r = obj.GetComponent<Renderer>();

        r.material.EnableKeyword("_EMISSION");
        r.material.SetColor("_EmissionColor", new Color(255,255,255));
        r.material.DOColor(Color.black, "_EmissionColor",0.5f)
            .OnComplete(onFlashEnd);
    }

    void onFlashEnd() {
        state = State.IDLE;
        
    }


    public List<List<Pair<int,int>>> CheckWaysToWin(List<List<string>> outcomes)
    {
        List<string> winSymbols = new List<string>();
        List<List<string>> winningCombinations = new List<List<string>>();
        List<List<Pair<int,int>>> winningPositions = new List<List<Pair<int,int>>>();

        // Iterate over all possible starting symbols in the first column
        for (int row = 0; row < numRows; row++)
        {
            string startingSymbol = outcomes[0][row];
            int storedRow = 0;

            List<string> currentCombination = new List<string> { startingSymbol };
            List<Pair<int,int>> currentPositions = new List<Pair<int,int>>();
            currentPositions.Add(new Pair<int,int>(0,row));
            
            

            // Check if this symbol continues across adjacent reels
            bool isWinning = true;
            for (int col = 1; col < numCols; col++)
            {
                bool matchFound = false;

                // Check all rows in the current column for a match
                for (int matchRow = 0; matchRow < numRows; matchRow++)
                {
                    if (outcomes[col][matchRow] == startingSymbol)
                    {
                        matchFound = true;
                        currentPositions.Add(new Pair<int,int>(col,matchRow));
                    }
                }

                if (matchFound)
                {
                    currentCombination.Add(startingSymbol); 
                    
                }
                else
                {
                    if (col <= 2) {
                        isWinning = false;
                    }
                    break;
                }
            }

            // If it's a winning combination, add it to the list
            if (isWinning)
            {
                winSymbols.Add(startingSymbol);
                winningCombinations.Add(new List<string>(currentCombination));
                winningPositions.Add(currentPositions);
            }
        }

        if (winSymbols.Count > 0) {
            winningSymbols = winSymbols;
        }

        return winningPositions;
    }
}
