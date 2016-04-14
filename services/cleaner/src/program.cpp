#include "program.h"

#include <boost/filesystem.hpp>
#include <boost/filesystem/fstream.hpp>
#include <boost/archive/text_iarchive.hpp>
#include <boost/archive/text_oarchive.hpp>

TProgram::TProgram() {
}

TProgram::TProgram(std::string& name, std::string& pass, std::string& listing)
    : TPassChecker(std::move(pass))
    , Name(std::move(name))
    , Listing(std::move(listing))
{
}

const std::string& TProgram::GetName() const {
    return Name;
}

const std::string& TProgram::GetListing() const {
    return Listing;
}

std::string TProgram::Run(const TRoom& room) const {
    std::string result = "runned!";
    return std::move(result);
}