using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using Phyrexia.ScamDetectorWpf.Services;

namespace Phyrexia.ScamDetectorWpf.ViewModels;

public class MainWindowViewModel : BindableBase
{
    public MainWindowViewModel()
    {
        ImportDeck = new DelegateCommand(ImportCmd);
        GenerateHand = new DelegateCommand(GenerateHandCmd, () => SampleHand.Count < Deck.Count);
        DrawCard = new DelegateCommand(DrawCardCmd, () => Deck.Count > 0);
        CalculateProbabilityCommand = new DelegateCommand(async () => await CalculateProbability(), () => IsReady && ConditionList.Count > 0);
        AddNewCondition = new DelegateCommand(() => ConditionList.Add(new (this)));
        IsConstructed = true;
        ConditionList.Add(new ConditionViewModel(this));
        IsReady = true;
    }

    #region Props

    public bool IsYorionCompanion { get; set; }

    public int CardsDrawnCount { get; set; } = 7;
        
    public bool AllOccurs { get; set; } = true;

    public bool IsReady
    {
        get => isReady;
        set
        {
            SetProperty(ref isReady, value);
            RaisePropertyChanged(nameof(IsRunning));
        }
    }

    public bool IsRunning => !isReady;

    public double Progress
    {
        get => progress;
        private set => SetProperty(ref progress, value, nameof(Progress));
    }

    public bool IsConstructed
    {
        get => isConstructed;
        set => SetProperty(ref isConstructed, value, nameof(IsConstructed));
    }

    public ObservableCollection<string> History { get; } = new ();
    public ObservableCollection<string> SampleHand { get; private set; } = new ();
    public ObservableCollection<ConditionViewModel> ConditionList { get; } = new ();
    
    public List<string> Deck { get; private set; } = new (capacity: 80);
    #endregion Props
    
    #region Commands
    public DelegateCommand CalculateProbabilityCommand { get; }
    public DelegateCommand AddNewCondition { get; }
    public DelegateCommand ImportDeck { get; }
    public DelegateCommand GenerateHand { get; }
    public DelegateCommand DrawCard { get; }
    

    #endregion Commands
    
    private void DrawCardCmd()
    {
        Debug.Assert(SampleHand.Count < Deck.Count);
        SampleHand.Add(Deck[SampleHand.Count]);
        DrawCard.RaiseCanExecuteChanged();
    }

    private void GenerateHandCmd()
    {
        Deck = Deck.OrderBy(_ => random.Next()).ToList();
        RaisePropertyChanged(nameof(Deck));
        SampleHand = new (Deck.Take(7));
        RaisePropertyChanged(nameof(SampleHand));
    }

    private void ImportCmd()
    {
        var import = Clipboard.GetText();
        var lines = import.Split([ '\r', '\n' ], StringSplitOptions.RemoveEmptyEntries)
                                 .SkipWhile(l => l != "Deck")
                                 .Skip(1)  // header
                                 .ToArray();
        if (lines.Length == 0)
        {
            Trace.TraceError($"Could not import deck: {import}");
            return;
        }

        Deck.Clear();
        SampleHand = new (lines.Skip(1).TakeWhile(s => s != "Sideboard"));
        Regex r = new (@"(\d) (.*)");
        foreach(var line in SampleHand) 
        {
            var rex = r.Match(line);
            if (rex.Groups.Count != 3 || !int.TryParse(rex.Groups[1].Value, out var num))
                break;
            
            for (var i = 0; i < num; i++)
                Deck.Add(rex.Groups[2].Value);
        }
        
        DrawCard.RaiseCanExecuteChanged();
        GenerateHand.RaiseCanExecuteChanged();
        RaisePropertyChanged(string.Empty);
    }
    
    
    private async Task CalculateProbability()
    {
        Debug.Assert(ConditionList.Count > 0);
        IsReady = false;
        Progress = 0;
        var probability = await CalculateProbabilityImpl().ConfigureAwait(true);
        IsReady = true;

        if (ConditionList.Count == 1)
        {
            History.Add($"==В {CardsDrawnCount} картах не менее {ConditionList[0].DublicateCount} из {ConditionList[0].CardsInDeckCount} карт: {probability:P2}==");
            return;
        }

        var delim = AllOccurs ? "И " : "ИЛИ ";
        string conditions = ConditionList.All(c => c.IsSimple)
            ? string.Join($" {delim}", ConditionList.Select(c => c.Name))
            : string.Join($"\n{delim}", ConditionList);
        var title = AllOccurs ? "ВСЕ события" : "ЛЮБОЕ событие";
        History.Add($"========== {title} в {CardsDrawnCount} картах ==========\n{conditions}\n{probability:P2}");
    }

    private async Task<double> CalculateProbabilityImpl()
    {
        var cardsInDeck = IsConstructed ? 60 : 40;
        if (IsYorionCompanion)
            cardsInDeck += 20;

        List<Guid> deck = new (capacity: cardsInDeck);
        deck.AddRange(ConditionList.SelectMany(c => Enumerable.Repeat(c.CardId, c.CardsInDeckCount)));
        deck.AddRange(Enumerable.Repeat(Guid.Empty, cardsInDeck - deck.Count));
        DeepShuffler shuffler = new ();
        var good = 0;
        const int cycles = 100;
        const int triesPerCycle = 4000;
        foreach (var cycle in Enumerable.Range(0, cycles))
        {
            foreach (var _ in Enumerable.Repeat(0, triesPerCycle))
            {
                deck = await shuffler.StupidShuffleAsync(deck).ConfigureAwait(false);
                if (AllOccurs)
                {
                    if (ConditionList.All(
                            c => deck.Take(CardsDrawnCount).Count(j => j == c.CardId) >= c.DublicateCount))
                        good++;
                }
                else
                {
                    if (ConditionList.Any(
                            c => deck.Take(CardsDrawnCount).Count(j => j == c.CardId) >= c.DublicateCount))
                        good++;
                }
            }

            Application.Current.Dispatcher.Invoke(() => Progress = cycle);
        }

        return good / (double)(triesPerCycle * cycles);
    }
    
    private bool isReady;
    private bool isConstructed = true;
    private readonly Random random = new();
    private double progress;
}