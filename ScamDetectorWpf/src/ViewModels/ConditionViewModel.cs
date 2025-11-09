namespace Phyrexia.ScamDetectorWpf.ViewModels;

public class ConditionViewModel : BindableBase
{
    public ConditionViewModel(MainWindowViewModel parent)
    {
        DeleteCondition = new DelegateCommand(
            () => parent.ConditionList.Remove(this), 
            () => parent.ConditionList.Count > 1);
        
        parent.ConditionList.CollectionChanged += delegate
        {
            DeleteCondition.RaiseCanExecuteChanged();
        };
    }

    public bool IsSimple => DublicateCount == 1 && !string.IsNullOrWhiteSpace(Name);

    public override string ToString()
    {
        if (IsSimple)
            return Name;

        return $"не менее {DublicateCount} из {CardsInDeckCount} {Name}";
    }

    public string Name { get; set; } = string.Empty;

    public Guid CardId { get; } = Guid.NewGuid();

    public int CardsInDeckCount { get; set; } = 4;
    public int DublicateCount { get; set; } = 1;
    public DelegateCommand DeleteCondition { get; }
}