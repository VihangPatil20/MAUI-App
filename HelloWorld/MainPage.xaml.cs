namespace HelloWorld;

public partial class MainPage : ContentPage
{
public MainPage()
{
InitializeComponent();
}

cd .
private void OnHelloClicked(object sender, EventArgs e)
{
    MessageLabel.Text = "Hello from my Android app!";
}


}
