#include "parser.h"

TCommandParser::TCommandParser(const std::string& source)
    : Source(source)
    , Idx(0)
    , State(EWaitCmd)
{
}

std::unique_ptr<ICommand> TCommandParser::GetNext() {
    char cmd;
    size_t num = 0;

    while (Idx < Source.size()) {
        switch (State) {
            case EWaitCmd:
                cmd = Source[Idx];
                if (cmd == 'P') {
                    State = EWaitChar; 
                } else {
                    State =  EWaitNumber1;
                }
                break;
            case EWaitNumber1:
                if (!GetNum(num)) {
                    return std::unique_ptr<ICommand>(new TErrorCommand());
                }
                if (cmd == 'N') {
                    State = EWaitNumber2; 
                } else {
                    Idx++;
                    State = EWaitCmd; 
                    return std::unique_ptr<ICommand>(new TMoveCommand(cmd, num));
                }
                break;
            case EWaitNumber2:
                size_t num2;
                if (!GetNum(num2)) {
                    return std::unique_ptr<ICommand>(new TErrorCommand());
                }
                Idx++;
                State = EWaitCmd; 
                return std::unique_ptr<ICommand>(new TNewCommand(num, num2));
            case EWaitChar:
                char c;
                c = Source[Idx];
                Idx++;
                State = EWaitCmd;
                return std::unique_ptr<ICommand>(new TPrintCommand(c));
            case EError:
                Idx++;
                return std::unique_ptr<ICommand>(new TErrorCommand());
        };
        Idx++;
    };
    return nullptr;
}

bool TCommandParser::GetNum(size_t& num) {
    char buf = Source[Idx]; 
    if (buf < '0' || buf > '9') {
        return false;
    }
    num = 10 * (buf - '0');
    Idx++;
    buf = Source[Idx]; 
    if (buf < '0' || buf > '9') {
        return false;
    }
    num += (buf - '0');
    return true; 
}
