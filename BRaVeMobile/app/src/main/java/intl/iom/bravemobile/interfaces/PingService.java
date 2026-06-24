package intl.iom.bravemobile.interfaces;

import java.util.function.Consumer;

public interface PingService {

    interface PingCallback  {
        void onSuccess();
        void onFailure( Throwable t);
    }
    //boolean basicPing();
    void basicPing(PingCallback  callback);
    void securePing(PingCallback  callback);
}
