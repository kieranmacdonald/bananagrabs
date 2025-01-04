import { useState } from 'react'
import './App.css'

function App() {
  const [image, setImage] = useState();

  function handleChange(e) {
    setImage(URL.createObjectURL(e.target.files[0]));
  }

  return (
    <>
      <img src={image} />
      <br />
      <input type="file" onChange={handleChange} />

    </>
  );
}

export default App
