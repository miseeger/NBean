namespace NBean.Interfaces {

    public interface IBeanDispenser {
        Bean Dispense(string kind);
        T Dispense<T>() where T : Bean, new();
    }

}
