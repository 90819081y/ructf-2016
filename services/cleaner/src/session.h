#pragma once

#include <string>
#include <sstream>

#include <boost/asio.hpp>

class TSession {
private:
    template <typename T>
    void WriteImpl(std::ostream& os, const T& first) {
        os << first;
    }

    template <typename T, typename ... Args>
    void WriteImpl(std::ostream& os, const T& first, Args&... args) {
        os << first;
        WriteImpl(os, args...);
    }

    bool ReadSocket(std::string& data);
    bool WriteSocket(const std::string& data);

public:
    TSession(boost::asio::ip::tcp::socket& sock)
        : Socket(sock)
    {
    }

    bool ReadLinesImpl(std::string& data) {
        return ReadSocket(data);
    }

    template <typename ... Args>
    bool ReadLinesImpl(std::string& data, Args&... args) {
        if (ReadSocket(data)) {
            return ReadLinesImpl(args...);
        }
        return false;
    }

    template <typename ... Args>
    void ReadLines(Args&... args) {
        bool result = ReadLinesImpl(args...);

        if (!result) {
            throw std::runtime_error("unexpected end of data");
        }
    }

    template <typename ... Args>
    bool Write(Args&... args) {
        std::stringstream out;
        WriteImpl(out, args...);
        return WriteSocket(out.str());
    }

private:
    boost::asio::ip::tcp::socket& Socket;
    boost::asio::streambuf Response;
};
