namespace QuadcopterLibrary;

public delegate void QuadcopterEventHandler(Quadcopter quadcopter);

public delegate void StateChangedEventHandler(Quadcopter quadcopter, QuadcopterState oldState, QuadcopterState newState);

public delegate void RemoteEventHandler(Operator remoteOperator);

public delegate void MechanicEventHandler(IMechanic mechanic, Quadcopter quadcopter);
