using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class GameView : BaseView
{
    [SerializeField] private Button _btn_Exit;

    private GameViewModel _viewModel = new();

    private void Start()
    {
        _btn_Exit.OnClickAsObservable().First().Subscribe(_ => _viewModel.OnExit());
    }
}
