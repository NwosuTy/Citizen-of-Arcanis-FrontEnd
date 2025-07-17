using System.Collections.Generic;

public interface IFuturePathProvider
{
    /// <summary>
    /// Predicts a list of Portions and Times the Vehicle plans to occupy in the future
    /// </summary>
    /// <param name="lookAheadSeconds">How many seconds into the future to predict</param>
    /// <param name="timeStepSeconds">Time interval between prediction points</param>
    /// <returns>A List of Path Reservations</returns>
    List<PathReservation> PredictFuturePath(float lookAheadSeconds, float timeStepSeconds);
}

public interface IInteractable
{
    public void Interact();
    public string GetInteractText();
}