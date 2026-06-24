package intl.iom.bravemobile.helpers;

import intl.iom.bravemobile.interfaces.LoadingHost;

public class WithLoading {
    private final LoadingHost host;

    public WithLoading(LoadingHost host) {
        this.host = host;
    }

    public interface Callback<T> {
        void onSuccess(T result);
        void onFailure(Throwable t);
    }

    public interface Call<T> {
        void invoke(Callback<T> cb);
    }

    public interface ResultHandler<T> {
        void onSuccess(T result);
        void onFailure(Throwable t);
    }

    /** Run without message (uses host.showLoading()). */
    public <T> void run(Call<T> call, ResultHandler<T> handler) {
        host.showLoading();
        try {
            call.invoke(new Callback<T>() {
                @Override
                public void onSuccess(T result) {
                    host.hideLoading();
                    handler.onSuccess(result);
                }

                @Override
                public void onFailure(Throwable t) {
                    host.hideLoading();
                    handler.onFailure(t);
                }
            });
        } catch (Throwable t) {
            host.hideLoading();
            handler.onFailure(t);
        }
    }

    public <T> void run(String message, Call<T> call) {
        if (host instanceof DialogLoadingHost) {
            ((DialogLoadingHost) host).showLoading(message);
        } else {
            host.showLoading();
        }
        try {
            call.invoke(new Callback<T>() {
                @Override
                public void onSuccess(T result) {
                    host.hideLoading();
                }

                @Override
                public void onFailure(Throwable t) {
                    host.hideLoading();
                }
            });
        } catch (Throwable t) {
            host.hideLoading();
        }
    }

    /** Run with a message (if host supports showing a message). */
    public <T> void run(String message, Call<T> call, ResultHandler<T> handler) {
        if (host instanceof DialogLoadingHost) {
            ((DialogLoadingHost) host).showLoading(message);
        } else {
            host.showLoading();
        }
        try {
            call.invoke(new Callback<T>() {
                @Override
                public void onSuccess(T result) {
                    host.hideLoading();
                    handler.onSuccess(result);
                }

                @Override
                public void onFailure(Throwable t) {
                    host.hideLoading();
                    handler.onFailure(t);
                }
            });
        } catch (Throwable t) {
            host.hideLoading();
            handler.onFailure(t);
        }
    }
}
